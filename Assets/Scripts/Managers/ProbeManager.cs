using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Managers
{
	public class ProbeManager : MonoBehaviour
	{
		public static ProbeManager Instance;
		
		private readonly Dictionary<ReflectionProbe, CancellationTokenSource> probes = new ();
		
		public void Awake()
		{
			Instance = this;
		}

		public void UpdateProbe(ReflectionProbe probe)
		{
			if (probes.TryGetValue(probe, out var source))
				source?.Cancel();

			probes[probe] = new CancellationTokenSource();
			updateProbe(probe, probes[probe].Token).Forget();
		}

		private async UniTask updateProbe(ReflectionProbe prob, CancellationToken token)
		{
			prob.mode = ReflectionProbeMode.Realtime;
			prob.refreshMode = ReflectionProbeRefreshMode.EveryFrame;

			await UniTask.WaitForSeconds(0.25f, cancellationToken: token);

			prob.refreshMode = ReflectionProbeRefreshMode.OnAwake;
		}
	}
}