using Components;
using Components.Enums;
using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class TextWalkerState
	{
		[JsonProperty]
		public ETextWalkerState State;
		
		[JsonProperty]
		public float StateChangeElapsed;

		[JsonProperty]
		public string Text;
		
		[JsonProperty]
		public int Character;

		[JsonProperty]
		public float StartDelay;
		
		[JsonProperty]
		public float EndDelay;
		
		[JsonProperty]
		public float StartCharacterDelay;
		
		[JsonProperty]
		public float EndCharacterDelay;
		
		public static TextWalkerState Read(TextWalker textWalker)
		{
			if (textWalker == null)
				return null;

			var state = new TextWalkerState
			{
				State = textWalker.CurrentState,
				StateChangeElapsed = textWalker.CurrentState is ETextWalkerState.Idle or ETextWalkerState.Done ? 0f : Time.time - textWalker.CurrentStateChangeTime,
				Text = textWalker.CurrentText,
				Character = textWalker.CurrentCharacter,
				StartDelay = textWalker.CurrentStartDelay,
				EndDelay = textWalker.CurrentEndDelay,
				StartCharacterDelay = textWalker.CurrentStartCharacterDelay,
				EndCharacterDelay = textWalker.CurrentEndCharacterDelay
			};

			return state;
		}
		
		public static void Apply(TextWalker textWalker, TextWalkerState state)
		{
			if (textWalker == null)
				return;

			textWalker.SetState(state.State, state.StateChangeElapsed, state.Text, state.Character, state.StartDelay, state.EndDelay, state.StartCharacterDelay, state.EndCharacterDelay);
		}
	}
}