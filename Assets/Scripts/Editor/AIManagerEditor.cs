using AI.Enums;
using Managers;
using Tools;
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
				var ray = aiManager.Player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
				var pos = Vector3.zero;

				if (Physics.Raycast(ray, out var hit, float.MaxValue, ~LayerMaskTools.GetMask()))
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
				var ray = aiManager.Player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
				var pos = Vector3.zero;

				if (Physics.Raycast(ray, out var hit, float.MaxValue, ~LayerMaskTools.GetMask()))
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
			
			if (GUILayout.Button("Find and Kill"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.FindAndKill(npc.Target);
				}
			}
						
			if (GUILayout.Button("Aiming Turret"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.AimingTurret(npc.Target);
				}
			}
			
			if (GUILayout.Button("Rage Turret"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.RageTurret();
				}
			}
			
			if (GUILayout.Button("Deathmatch"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.SenseRange = 9999;
					
					npc.AutoTargetRange = 9999;
					npc.AutoTarget = EAutoTarget.NPCs;
					
					npc.FindAndKill(npc.Target, false, true);
				}
			}

			GUILayout.EndHorizontal();
			
			GUILayout.Label("Misc", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			
			EditorGUIUtility.labelWidth = 75f;
			Weapon = EditorGUILayout.TextField("Weapon", Weapon);
			EditorGUIUtility.labelWidth = 0f;

			if (GUILayout.Button("Give Player"))
			{
				aiManager.Player.TakeWeapon(ObjectManager.Instance.CreateWeapon(TypeTools.FindType(Weapon), Vector3.zero, Vector3.zero));
			}
			
			if (GUILayout.Button("Give NPCs"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;
					
					npc.TakeWeapon(ObjectManager.Instance.CreateWeapon(TypeTools.FindType(Weapon), Vector3.zero, Vector3.zero));
				}
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.Label("Auto Target", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("None"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.AutoTarget = EAutoTarget.None;
				}
			}
			
			if (GUILayout.Button("Player"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.AutoTarget = EAutoTarget.Player;
				}
			}
			
			if (GUILayout.Button("NPCs"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.AutoTarget = EAutoTarget.NPCs;
				}
			}
			
			if (GUILayout.Button("Everyone"))
			{
				foreach (var npc in aiManager.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.AutoTarget = EAutoTarget.Player | EAutoTarget.NPCs;
				}
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