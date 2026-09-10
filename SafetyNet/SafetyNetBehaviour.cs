using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SafetyNet
{
	public partial class SafetyNet
	{
		public class SafetyNetBehaviour : MonoBehaviour , IComparable< SafetyNetBehaviour >
		{
			public static WireframeMeshHelper WireframeMeshFactory { get; private set; } = new WireframeMeshHelper();
			protected static List< SafetyNetBehaviour > SafetyNets { get; private set; } = new List< SafetyNetBehaviour >();
			protected static Shader MaterialShader = null;
			protected static Mesh SharedSphereMesh = new Mesh();

			public Collider collider { get; private set; } = null;
			public bool considerForRender { get; private set; } = false;
			public EffectArea effectArea { get; private set; }
			public EffectArea.Type effectAreaType { get; private set; }
			public MeshRenderer meshRenderer { get; private set; } = null;
			public MeshFilter meshFilter{ get; private set; } = null;
			public CircleProjector projector { get; private set; } = null;
			protected float distanceFactor { get; private set; } = float.PositiveInfinity;
			protected float capsuleColliderRadius { get; private set; } = -1.0f;

			static SafetyNetBehaviour()
			{
				WireframeMeshFactory.latitudeFidelity = LatitudeFidelity.Value;
				WireframeMeshFactory.longitudeFidelity = LongitudeFidelity.Value;
				WireframeMeshFactory.RegenerateMeshForSphere( SharedSphereMesh );
			}

			public void Start()
			{
				GameObject parent = gameObject.transform.parent.gameObject;
				effectArea = GetWidestEffectAreaOfTypeOn( effectAreaType , parent );
				collider = GetColliderFromEffectArea( effectArea );
				projector = gameObject.transform.root.GetComponentInChildren< CircleProjector >( true );

				MaterialShader = MaterialShader ?? Resources.FindObjectsOfTypeAll< Shader >().FirstOrDefault( x => x.name == "Unlit/Color" );
				Material material = new Material( MaterialShader );
				float red = 0.5f + Mathf.Abs( gameObject.transform.position.x % 0.5f );
				float green = 0.5f + Mathf.Abs( gameObject.transform.position.y % 0.5f );
				float blue = 0.5f + Mathf.Abs( gameObject.transform.position.z % 0.5f );
				material.SetColor( "_Color" , new Color( red , green , blue ) );

				meshRenderer = gameObject.AddComponent< MeshRenderer >();
				meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				meshRenderer.receiveShadows = false;
				meshRenderer.material = material;
				meshRenderer.enabled = false;

				meshFilter = gameObject.AddComponent< MeshFilter >();
				CreateOrUpdateMesh();

				SafetyNets.Add( this );
			}

			public void FixedUpdate()
			{
				if( !IsEnabled.Value )
				{
					considerForRender = false;
					meshRenderer.enabled = false;
					if( projector )
					{
						projector.enabled = false;
						projector.gameObject.SetActive( false );
					}
				}
			}

			public void OnDestroy()
			{
				SafetyNets.Remove( this );
				collider = null;
				effectArea = null;
				meshRenderer = null;
				meshFilter = null;
				projector = null;
			}

			protected bool CreateOrUpdateMesh()
			{
				if( collider is SphereCollider )
				{
					if( meshFilter.sharedMesh != SharedSphereMesh )
					{
						meshFilter.mesh = null;
						meshFilter.sharedMesh = SharedSphereMesh;
						return true;
					}
				}
				else if( collider is CapsuleCollider capsuleCollider )
				{
					if( capsuleCollider.radius != capsuleColliderRadius )
					{
						meshFilter.sharedMesh = null;
						meshFilter.mesh = meshFilter.mesh ?? new Mesh();
						WireframeMeshFactory.RegenerateMeshForCapsule( meshFilter.mesh , capsuleCollider );
						capsuleColliderRadius = capsuleCollider.radius;
						return true;
					}
				}

				// TODO: Draw a bounding box around the associated piece for improved visibility?
				// The mess (or sparsity) of lines can make it difficult to tell what's what.
				return false;
			}

			protected void UpdateRenderConsiderations()
			{
				// We cannot check if the parent is active.
				// Some effect areas are attached to permanently inactive objects.
				if( IsEnabled.Value && Player.m_localPlayer && effectArea )
				{
					// Prod crafting stations to query upgrades as their range depends on it
					gameObject.transform.root.GetComponentsInChildren< CraftingStation >()
						.FirstOrDefault()
						?.GetLevel();

					// TODO: Check the collider for the closest point?
					float radius = effectArea.GetRadius();
					float renderDistanceLimit = Mathf.Min( radius , 50.0f ) * 2.0f;
					float renderDistanceLimitSquared = renderDistanceLimit * renderDistanceLimit;
					Vector3 playerPos = Player.m_localPlayer.transform.position;
					float playerDistanceFromCenterSquared = ( playerPos - transform.position ).sqrMagnitude;
					if( playerDistanceFromCenterSquared <= renderDistanceLimitSquared )
					{
						// Objects with a large effect radius may dominate nearby objects with a smaller radius
						// due to distanceFactor being smaller as the player is "deeper in the well".
						// This shows more volume, but is slightly misleading as to what objects provide safety.
						distanceFactor = playerDistanceFromCenterSquared - ( radius * radius );
						considerForRender = true;

						if( collider is SphereCollider )
							gameObject.transform.localScale = new Vector3( radius , radius , radius );
						else if( CreateOrUpdateMesh() )
							gameObject.transform.localScale = Vector3.one;

						return;
					}
				}

				considerForRender = false;
			}

			// IComparable overrides

			public int CompareTo( SafetyNetBehaviour other )
			{
				return distanceFactor < other.distanceFactor ? -1 : other.distanceFactor < distanceFactor ? 1 : 0;
			}

			// Statics

			public static void CreateOnFor( GameObject parent , EffectArea effectArea )
			{
				string namePrefix = effectArea.m_type == EffectArea.Type.PlayerBase ? "PlayerBase" : "NoMonsters";
				GameObject go = new GameObject( $"{namePrefix}SafetyNet" );
				go.transform.parent = parent.transform;
				go.transform.localPosition = new Vector3( 0.0f , 0.0f , 0.0f );
				go.transform.localRotation = Quaternion.identity;
				go.transform.localScale = new Vector3( 1.0f , 1.0f , 1.0f );

				SafetyNetBehaviour safetyNet = go.AddComponent< SafetyNetBehaviour >();
				safetyNet.effectAreaType = effectArea.m_type;
			}

			protected static Collider GetColliderFromEffectArea( EffectArea effectArea )
			{
				return Traverse.Create( effectArea )
					.Field( "m_collider" )
					.GetValue< Collider >();
			}

			protected static EffectArea GetWidestEffectAreaOfTypeOn( EffectArea.Type effectAreaType , GameObject go )
			{
				// Effect area colliders may change size which could be misleading.
				// Hopefully a game object doesn't have multiple areas of the same type.
				return go.GetComponentsInChildren< EffectArea >()
					.Where( x => x.m_type == effectAreaType )
					.OrderByDescending( x => x.GetRadius() )
					.FirstOrDefault();
			}

			public static void UpdateMeshes()
			{
				WireframeMeshFactory.RegenerateMeshForSphere( SharedSphereMesh );

				foreach( SafetyNetBehaviour safetyNet in SafetyNets )
					safetyNet.CreateOrUpdateMesh();
			}

			public static IEnumerable< SafetyNetBehaviour > UpdateRenderConsiderationsAndSort()
			{
				foreach( SafetyNetBehaviour safetyNet in SafetyNets )
					safetyNet.UpdateRenderConsiderations();

				SafetyNets.Sort();
				return SafetyNets;
			}
		}
	}
}
