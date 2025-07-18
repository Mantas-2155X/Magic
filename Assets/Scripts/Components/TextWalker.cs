using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Components.Enums;
using Components.Events;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using State.Enums;
using State.Interfaces;
using State.States;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Components
{
	public class TextWalker : MonoBehaviour, ISaveable
	{
		[SerializeField]
		public TMP_Text Text;
		
		[SerializeField]
		public OnTextWalkerFinishedEvent OnTextWalkerFinishedEvent = new ();
		
		private ETextWalkerState currentState;
		public ETextWalkerState CurrentState
		{
			get => currentState;
			private set
			{
				CurrentStateChangeTime = Time.time;
				currentState = value;
			}
		}
		public float CurrentStateChangeTime { get; private set; }

		public string CurrentText { get; private set; }
		public int CurrentCharacter { get; private set; }
		
		public float CurrentStartDelay { get; private set; }
		public float CurrentEndDelay { get; private set; }
		
		public float CurrentStartCharacterDelay { get; private set; }
		public float CurrentEndCharacterDelay { get; private set; }
		
		private CancellationTokenSource cancellationToken = new ();
		
		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		#region Identify / SaveLoad
		
		public virtual bool ShouldSave => true;
		
		public virtual ELoadType LoadType => ELoadType.Modify;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.Normal;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}
		
		public virtual JObject GetCreation()
		{
			throw new NotImplementedException();
		}
		
		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(TextWalker).ToString()] = JObject.FromObject(new TextWalkerState(this));
			
			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(TextWalker).ToString(), out var textWalkerState) && textWalkerState != null)
				textWalkerState.ToObject<TextWalkerState>().Apply(this);
		}
		
		public void SetState(ETextWalkerState state, float stateChangeElapsed, string text, int character, float startDelay, float endDelay, float startCharacterDelay, float endCharacterDelay)
		{
			if (state is ETextWalkerState.Idle or ETextWalkerState.Done)
			{
				cancellationToken?.Cancel();
				
				CurrentState = state;
				
				CurrentText = "";
				CurrentCharacter = 0;

				Text.text = "";
				return;
			}

			switch (state)
			{
				case ETextWalkerState.StartWait:
					startDelay -= stateChangeElapsed;
					break;
				case ETextWalkerState.EndWait:
					endDelay -= stateChangeElapsed;
					break;
			}
			
			Walk(text, startDelay, endDelay, startCharacterDelay, endCharacterDelay, character, state);
		}
		
		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
			initializeObject();
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion

		public void Walk(string text, float startDelay, float endDelay, float startCharacterDelay, float endCharacterDelay, int startAtCharacter = 0, ETextWalkerState startAtState = ETextWalkerState.Idle)
		{
			if (cancellationToken != null)
				cancellationToken?.Cancel();

			CurrentState = startAtState;
			
			CurrentText = text;
			CurrentCharacter = startAtCharacter;
			
			CurrentStartDelay = startDelay;
			CurrentEndDelay = endDelay;
			
			CurrentStartCharacterDelay = startCharacterDelay;
			CurrentEndCharacterDelay = endCharacterDelay;

			Text.text = CurrentText[..(CurrentCharacter)];
			
			cancellationToken = new CancellationTokenSource();
			textLoop(cancellationToken.Token).Forget();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnTextWalkerFinishedEvent, Color.blue);
		}
#endif
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private void initializeObject()
		{
			if (init)
				return;

			thisGo = gameObject;
			thisTr = thisGo.transform;
			init = true;
		}
		
		private async UniTaskVoid textLoop(CancellationToken token)
		{
			if (CurrentState is ETextWalkerState.Idle or ETextWalkerState.StartWait)
				await startWait(token);

			if (CurrentState is ETextWalkerState.StartWait or ETextWalkerState.Starting)
				await startText(token);
			
			if (CurrentState is ETextWalkerState.Starting or ETextWalkerState.EndWait)
				await endWait(token);

			if (CurrentState is ETextWalkerState.EndWait or ETextWalkerState.Ending)
				await endText(token);
		}
		
		private async UniTask startWait(CancellationToken token)
		{
			CurrentState = ETextWalkerState.StartWait;
			await UniTask.WaitForSeconds(CurrentStartDelay, cancellationToken: token);
		}
		
		private async UniTask startText(CancellationToken token)
		{
			CurrentState = ETextWalkerState.Starting;

			while (CurrentCharacter < CurrentText.Length)
			{
				await UniTask.WaitForSeconds(CurrentStartCharacterDelay, cancellationToken: token);
				
				if (token.IsCancellationRequested || this == null || !isActiveAndEnabled)
					return;
				
				Text.text = CurrentText[..(CurrentCharacter + 1)];
				CurrentCharacter++;
			}
		}
		
		private async UniTask endWait(CancellationToken token)
		{
			CurrentState = ETextWalkerState.EndWait;
			await UniTask.WaitForSeconds(CurrentEndDelay, cancellationToken: token);
		}
		
		private async UniTask endText(CancellationToken token)
		{
			CurrentState = ETextWalkerState.Ending;

			while (CurrentCharacter >= 0)
			{
				await UniTask.WaitForSeconds(CurrentEndCharacterDelay, cancellationToken: token);
				
				if (token.IsCancellationRequested || this == null || !isActiveAndEnabled)
					return;
				
				Text.text = CurrentText[..CurrentCharacter];
				CurrentCharacter--;
			}
			
			CurrentState = ETextWalkerState.Done;
			OnTextWalkerFinishedEvent?.Invoke();
		}
		
		[JsonObject]
		public class TextWalkerState : IState
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

			public TextWalkerState() { }
			
			public TextWalkerState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not TextWalker textWalker)
					return;

				State = textWalker.CurrentState;
				StateChangeElapsed = textWalker.CurrentState is ETextWalkerState.Idle or ETextWalkerState.Done ? 0f : Time.time - textWalker.CurrentStateChangeTime;
				Text = textWalker.CurrentText;
				Character = textWalker.CurrentCharacter;
				StartDelay = textWalker.CurrentStartDelay;
				EndDelay = textWalker.CurrentEndDelay;
				StartCharacterDelay = textWalker.CurrentStartCharacterDelay;
				EndCharacterDelay = textWalker.CurrentEndCharacterDelay;
			}
			
			public void Apply(object obj)
			{
				if (obj is not TextWalker textWalker)
					return;

				textWalker.SetState(State, StateChangeElapsed, Text, Character, StartDelay, EndDelay, StartCharacterDelay, EndCharacterDelay);
			}
		}
	}
}