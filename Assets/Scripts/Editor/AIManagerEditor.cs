using System.Collections.Generic;
using AI.Enums;
using AI.Interfaces;
using Managers;
using Objects;
using Tools;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Editor
{
	[CustomEditor(typeof(AIManager))]
	public class AIManagerEditor : UnityEditor.Editor
	{
		[SerializeField]
		public string Weapon;
		
		public override void OnInspectorGUI()
		{
			var aiManager = AIManager.Instance;
			var world = World.World.Instance;
			
			if (aiManager == null || world == null)
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
				
				aiManager.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles);
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
				
				aiManager.CreateNPC(spawnPoint.position, spawnPoint.eulerAngles);
			}

			if (GUILayout.Button("Create at cam target"))
			{
				if (aiManager.Player == null)
					return;
				
				var ray = aiManager.Player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
				var pos = Vector3.zero;

				if (Physics.Raycast(ray, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
					pos = hit.point + Vector3.up * 1.5f;

				aiManager.CreateNPC(pos, Vector3.zero);
			}
			
			if (GUILayout.Button("Kill all"))
			{
				foreach (var npc in aiManager.NPCs)
				{
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
					npc.Walk(aiManager.Player.transform.position);
				}
			}

			if (GUILayout.Button("Camera target"))
			{
				if (aiManager.Player == null)
					return;

				var ray = aiManager.Player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
				var pos = Vector3.zero;

				if (Physics.Raycast(ray, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
					pos = hit.point + Vector3.up * 1.5f;
				
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Chill();
					npc.Walk(pos);
				}
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
			
			GUILayout.Label("Action", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Chase and Kill"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Attack(aiManager.Player, EActionMode.ChaseAndKill);
				}
			}
						
			if (GUILayout.Button("Aiming Turret"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Attack(aiManager.Player, EActionMode.AimingTurret);
				}
			}
			
			if (GUILayout.Button("Rage Turret"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Attack(null, EActionMode.RageTurret);
				}
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.Label("Misc", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			Weapon = EditorGUILayout.TextField(Weapon);
			if (GUILayout.Button("Give everyone Weapon"))
			{
				var trs = new List<Transform>();
				trs.Add(aiManager.Player.transform);
				
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					trs.Add(npc.transform);
				}

				foreach (var tr in trs)
				{
					var go = Instantiate(Resources.Load<GameObject>("Objects/DroppedWeapon"));
					var dropped = go.GetComponent<DroppedWeapon>();
					dropped.Weapon = Weapon;
					dropped.Pickup(tr.GetComponent<IAlive>());
				}
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.Label("Status:", EditorStyles.boldLabel);
			
			if (aiManager.Player != null)
			{
				GUILayout.BeginHorizontal();

				GUILayout.Label("Player", GUILayout.Width(50));
				GUILayout.Label($"{(aiManager.Player.IsAlive ? "Alive" : "Dead")}", GUILayout.Width(40));
				GUILayout.Label($"HP: {aiManager.Player.CurrentHealth}", GUILayout.Width(50));
				GUILayout.Label($"Vel: {aiManager.Player.Rigidbody.linearVelocity.magnitude:0.0000}", GUILayout.Width(80));
				GUILayout.FlexibleSpace();
				var invulnerable = GUILayout.Toggle(aiManager.Player.IsInvulnerable, "Invulnerable", GUILayout.Width(100));
				aiManager.Player.SetInvulnerable(invulnerable);
				var noclip = GUILayout.Toggle(aiManager.Player.IsNoclip, "Noclip", GUILayout.Width(75));
				aiManager.Player.SetNoclip(noclip);
				
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(20);
			
			foreach (var npc in aiManager.NPCs)
			{
				GUILayout.BeginHorizontal();

				GUILayout.Label(npc.gameObject.name, GUILayout.Width(50));
				GUILayout.Label($"{(npc.IsAlive ? "Alive" : "Dead")}", GUILayout.Width(40));
				GUILayout.Label($"HP: {npc.CurrentHealth}", GUILayout.Width(50));
				GUILayout.Label($"Vel: {npc.Agent.velocity.magnitude:0.0000}", GUILayout.Width(80));
				GUILayout.FlexibleSpace();
				var invulnerable = GUILayout.Toggle(npc.IsInvulnerable, "Invulnerable", GUILayout.Width(100));
				npc.SetInvulnerable(invulnerable);
				var noclip = GUILayout.Toggle(npc.IsNoclip, "Noclip", GUILayout.Width(75));
				npc.SetNoclip(noclip);
				
				GUILayout.EndHorizontal();
				
				GUILayout.BeginHorizontal();

				EditorGUIUtility.labelWidth = 20f;
				EditorGUILayout.EnumPopup("AI: ", npc.AIMode, GUILayout.Width(95f));
				EditorGUIUtility.labelWidth = 25f;
				EditorGUILayout.EnumPopup("Act: ", npc.ActionMode, GUILayout.Width(130f));
				EditorGUIUtility.labelWidth = 45f;
				EditorGUILayout.ObjectField("Target: ", npc.Target, typeof(Component), true);
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