﻿#define USEPATHWAITCOUNTERx

using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TrafficManager.Custom.AI;
using TrafficManager.State;
using TrafficManager.Traffic;
using UnityEngine;

namespace TrafficManager.Manager {
	public class ExtCitizenInstanceManager {
		private static ExtCitizenInstanceManager instance = null;

		public static ExtCitizenInstanceManager Instance() {
            if (instance == null)
				instance = new ExtCitizenInstanceManager();
			return instance;
		}

		static ExtCitizenInstanceManager() {
			Instance();
		}

		/// <summary>
		/// All additional data for citizen instance
		/// </summary>
		private ExtCitizenInstance[] ExtInstances = null;

		private ExtCitizenInstanceManager() {
			ExtInstances = new ExtCitizenInstance[CitizenManager.MAX_INSTANCE_COUNT];
			for (uint i = 0; i < CitizenManager.MAX_INSTANCE_COUNT; ++i) {
				ExtInstances[i] = new ExtCitizenInstance((ushort)i);
			}
		}

		/// <summary>
		/// Retrieves the additional citizen instance data for the given instance id.
		/// </summary>
		/// <param name="instanceId"></param>
		/// <returns>the additional citizen instance data</returns>
		public ExtCitizenInstance GetExtInstance(ushort instanceId) {
#if TRACE
			Singleton<CodeProfiler>.instance.Start("VehicleStateManager.GetVehicleState");
#endif
			ExtCitizenInstance ret = ExtInstances[instanceId];
#if TRACE
			Singleton<CodeProfiler>.instance.Stop("VehicleStateManager.GetVehicleState");
#endif
			return ret;
		}
		
		internal void OnLevelUnloading() {
			for (int i = 0; i < ExtInstances.Length; ++i)
				ExtInstances[i].Reset();
		}
	}
}
