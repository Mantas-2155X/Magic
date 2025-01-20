using System.Collections.Generic;
using AI.Enums;
using Managers;
using ScriptableObjects;
using Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Editor
{
	[CustomEditor(typeof(AIManager))]
	public class AIManagerEditor : UnityEditor.Editor
	{
		[SerializeField]
		public string Wearable;

		[SerializeField]
		public string Spell;

		[SerializeField]
		public bool ShowStats;
		
		public override void OnInspectorGUI()
		{
			var aiManager = AIManager.Instance;
			var world = World.World.Instance;
			
			if (aiManager == null || world == null || aiManager.Player == null)
			{
				base.OnInspectorGUI();
				serializedObject.ApplyModifiedProperties();
				return;
			}
			
			GUILayout.Label("Player", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Create"))
			{
				var spawnPoints = world.SpawnPoints;
				var spawnPoint = spawnPoints.GetChild(Random.Range(0, spawnPoints.childCount));
				
				aiManager.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, (PlayerData)ObjectManager.Instance.GetAlive("AI_PLAYER_NAME"));
			}
			
			if (GUILayout.Button("Kill"))
			{
				aiManager.Player.Kill(null);
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.Label("NPC", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Create"))
			{
				var spawnPoints = world.SpawnPoints;
				var spawnPoint = spawnPoints.GetChild(Random.Range(0, spawnPoints.childCount));
				
				aiManager.CreateNPC(spawnPoint.position, spawnPoint.eulerAngles, (NPCData)ObjectManager.Instance.GetAlive("AI_NPC_NAME"));
			}

			if (GUILayout.Button("Create at cam target"))
			{
				var ray = aiManager.Player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
				var pos = Vector3.zero;

				if (Physics.Raycast(ray, out var hit, float.MaxValue, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
					pos = hit.point + Vector3.up * 1.5f;

				aiManager.CreateNPC(pos, Vector3.zero, (NPCData)ObjectManager.Instance.GetAlive("AI_NPC_NAME"));
			}
			
			if (GUILayout.Button("Kill all"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Kill(null);
				}
			}

			GUILayout.EndHorizontal();
			
			GUILayout.Label("Go to", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Player"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Chill();
					npc.Walk(aiManager.Player.GetTransform().position);
				}
			}

			if (GUILayout.Button("Camera target"))
			{
				var ray = aiManager.Player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
				var pos = Vector3.zero;

				if (Physics.Raycast(ray, out var hit, float.MaxValue, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
					pos = hit.point + Vector3.up * 1.5f;
				
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Chill();
					npc.Walk(pos);
				}

				Debug.Log(pos);
			}
			
			if (GUILayout.Button("Random spawnpoint"))
			{
				var spawnPoints = world.SpawnPoints;
				var spawnPoint = spawnPoints.GetChild(Random.Range(0, spawnPoints.childCount));
				
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Chill();
					npc.Walk(spawnPoint.position);
				}
			}

			GUILayout.EndHorizontal();
			
			GUILayout.Label("Relationship Group", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("-1"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.SetRelationshipGroup(-1);
				}
			}
			
			if (GUILayout.Button("0"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.SetRelationshipGroup(0);
				}
			}
			
			if (GUILayout.Button("Random"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.SetRelationshipGroup(Random.Range(1, int.MaxValue));
				}
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.Label("Aggressive", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Yes"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.AssignAggressive(true);
				}
			}
			
			if (GUILayout.Button("No"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.AssignAggressive(false);
				}
			}

			GUILayout.EndHorizontal();
			
			GUILayout.Label("Action", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Wander"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Wander();
				}
			}
			
			if (GUILayout.Button("Patrol"))
			{
				var points = new List<Vector3>();
				points.Add(new Vector3(-28.50f, -0.93f, -16.63f));
				points.Add(new Vector3(-29.08f, 11.57f, -18.15f));

				var path = Path.FromVectors(points);
				
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;
					
					npc.Patrol(path);
				}
			}
			
			if (GUILayout.Button("Idle"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Idle();
				}
			}
			
			if (GUILayout.Button("Deathmatch"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;
					
					npc.SetRelationshipGroup(Random.Range(1, int.MaxValue));
					npc.Wander();
				}
			}

			GUILayout.EndHorizontal();
			
			GUILayout.Label("Misc", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			EditorGUIUtility.labelWidth = 75f;
			Wearable = EditorGUILayout.TextField("Wearable", Wearable);
			EditorGUIUtility.labelWidth = 0f;

			if (GUILayout.Button("Give Player"))
			{
				aiManager.Player.EquipWearable(ObjectManager.Instance.GetWearable(Wearable));
			}
			
			if (GUILayout.Button("Give NPCs"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;
					
					npc.EquipWearable(ObjectManager.Instance.GetWearable(Wearable));
				}
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			
			EditorGUIUtility.labelWidth = 75f;
			Spell = EditorGUILayout.TextField("Spell", Spell);
			EditorGUIUtility.labelWidth = 0f;

			if (GUILayout.Button("Give Player"))
			{
				aiManager.Player.LearnSpell(ObjectManager.Instance.GetSpell(Spell), true);
			}
			
			if (GUILayout.Button("Give NPCs"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;
					
					npc.LearnSpell(ObjectManager.Instance.GetSpell(Spell), true);
				}
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Refill Mana"))
			{
				aiManager.Player.RestoreMana(aiManager.Player.Data.Mana, null);

				foreach (var npc in aiManager.NPCs)
					npc.RestoreMana(npc.Data.Mana, null);
			}
			
			if (GUILayout.Button("Clear Mana"))
			{
				aiManager.Player.TakeMana(aiManager.Player.CurrentMana, null);

				foreach (var npc in aiManager.NPCs)
					npc.TakeMana(npc.CurrentMana, null);
			}
			
			GUILayout.EndHorizontal();
			
			ShowStats = EditorGUILayout.ToggleLeft("Show Stats", ShowStats);
			if (!ShowStats)
			{
				base.OnInspectorGUI();
				serializedObject.ApplyModifiedProperties();
				return;
			}

			GUILayout.Label("Status:", EditorStyles.boldLabel);
			
			{
				GUILayout.BeginHorizontal();

				GUILayout.Label("Player", GUILayout.Width(50));
				GUILayout.Label($"HP: {aiManager.Player.CurrentHealth}", GUILayout.Width(50));
				GUILayout.Label($"MP: {aiManager.Player.CurrentMana}", GUILayout.Width(50));
				GUILayout.Label($"Vel: {aiManager.Player.Body.Rigidbody.linearVelocity.magnitude:0.0000}", GUILayout.Width(80));
				aiManager.Player.SetInvulnerable(GUILayout.Toggle(aiManager.Player.IsInvulnerable, "INV", GUILayout.Width(50)));
				aiManager.Player.SetPowerful(GUILayout.Toggle(aiManager.Player.IsPowerful, "PW", GUILayout.Width(50)));
				aiManager.Player.SetMovementType((EMovementType)EditorGUILayout.EnumPopup(aiManager.Player.MovementType));
				
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(20);
			
			foreach (var npc in aiManager.NPCs)
			{
				if (!npc.IsAlive)
					continue;
				
				GUILayout.BeginHorizontal();

				GUILayout.Label(npc.gameObject.name, GUILayout.Width(50));
				GUILayout.Label($"HP: {npc.CurrentHealth}", GUILayout.Width(50));
				GUILayout.Label($"MP: {npc.CurrentMana}", GUILayout.Width(50));
				GUILayout.Label($"Vel: {npc.Agent.velocity.magnitude:0.0000}", GUILayout.Width(80));
				npc.SetInvulnerable(GUILayout.Toggle(npc.IsInvulnerable, "INV", GUILayout.Width(50)));
				npc.SetPowerful(GUILayout.Toggle(npc.IsPowerful, "PW", GUILayout.Width(50)));
				npc.SetMovementType((EMovementType)EditorGUILayout.EnumPopup(npc.MovementType));
				
				GUILayout.EndHorizontal();
				
				GUILayout.BeginHorizontal();

				EditorGUIUtility.labelWidth = 20f;
				EditorGUILayout.EnumPopup("AI: ", npc.AIMode, GUILayout.Width(95f));
				EditorGUIUtility.labelWidth = 25f;
				EditorGUILayout.EnumPopup("Act: ", npc.ActionMode, GUILayout.Width(130f));
				EditorGUIUtility.labelWidth = 45f;
				EditorGUILayout.ObjectField("Attack: ", npc.AttackTarget, typeof(Component), true);
				EditorGUIUtility.labelWidth = 0f;

				GUILayout.EndHorizontal();
			}
			
			GUILayout.Space(10);
			
			base.OnInspectorGUI();
			serializedObject.ApplyModifiedProperties();
			
			Repaint();
		}
	}
}