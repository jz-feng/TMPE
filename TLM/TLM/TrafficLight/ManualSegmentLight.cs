﻿using System;
using System.Collections.Generic;
using ColossalFramework;
using TrafficManager.Traffic;
using UnityEngine;

namespace TrafficManager.TrafficLight {
	public class ManualSegmentLight : ICloneable {
		public enum Mode {
			Simple = 1, // <^>
			SingleRight = 2, // <^, >
			SingleLeft = 3, // <, ^>
			All = 4 // <, ^, >
		}

		public ushort Node;
		public int Segment;

		public Mode CurrentMode = Mode.Simple;

		public RoadBaseAI.TrafficLightState LightLeft;
		public RoadBaseAI.TrafficLightState LightMain;
		public RoadBaseAI.TrafficLightState LightRight;
		public RoadBaseAI.TrafficLightState LightPedestrian;

		/// <summary>
		/// Left outgoing segment ids. If `LightLeft` is green, traffic may flow to those segments.
		/// </summary>
		private List<int> leftOutSegmentIds;
		/// <summary>
		/// Left outgoing segment ids. If `LightMain` is green, traffic may flow to those segments.
		/// </summary>
		private List<int> forwardOutSegmentIds;
		/// <summary>
		/// Left outgoing segment ids. If `LightRight` is green, traffic may flow to those segments.
		/// </summary>
		private List<int> rightOutSegmentIds;

		public uint LastChange;
		public uint LastChangeFrame;

		public bool PedestrianEnabled;

		public ManualSegmentLight(ushort nodeId, int segmentId, RoadBaseAI.TrafficLightState mainLight) {
			Node = nodeId;
			Segment = segmentId;

			leftOutSegmentIds = new List<int>();
			forwardOutSegmentIds = new List<int>();
			rightOutSegmentIds = new List<int>();

			LightMain = mainLight;
			LightLeft = mainLight;
			LightRight = mainLight;
			LightPedestrian = mainLight == RoadBaseAI.TrafficLightState.Green
				? RoadBaseAI.TrafficLightState.Red
				: RoadBaseAI.TrafficLightState.Green;

			// build outgoing segment lists
			var node = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId];

			for (var s = 0; s < 8; s++) {
				var toSegmentId = node.GetSegment(s);
				if (toSegmentId == 0) continue;

				if (TrafficPriority.IsLeftSegment(segmentId, toSegmentId, nodeId))
					leftOutSegmentIds.Add(toSegmentId);
				else if (TrafficPriority.IsRightSegment(segmentId, toSegmentId, nodeId))
					rightOutSegmentIds.Add(toSegmentId);
				else
					forwardOutSegmentIds.Add(toSegmentId);
			}

			UpdateVisuals();
		}

		public ManualSegmentLight(ushort nodeId, int segmentId, RoadBaseAI.TrafficLightState mainLight, RoadBaseAI.TrafficLightState leftLight, RoadBaseAI.TrafficLightState rightLight, RoadBaseAI.TrafficLightState pedestrianLight) {
			Node = nodeId;
			Segment = segmentId;

			LightMain = mainLight;
			LightLeft = leftLight;
			LightRight = rightLight;
			LightPedestrian = pedestrianLight;

			UpdateVisuals();
		}

		public RoadBaseAI.TrafficLightState GetLightMain() {
			return LightMain;
		}

		public RoadBaseAI.TrafficLightState GetLightLeft() {
			return LightLeft;
		}

		public RoadBaseAI.TrafficLightState GetLightRight() {
			return LightRight;
		}

		public RoadBaseAI.TrafficLightState GetLightPedestrian() {
			return LightPedestrian;
		}

		public void ChangeMode() {
			var hasLeftSegment = TrafficPriority.HasLeftSegment(Segment, Node) && TrafficPriority.HasLeftLane(Node, Segment);
			var hasForwardSegment = TrafficPriority.HasForwardSegment(Segment, Node) && TrafficPriority.HasForwardLane(Node, Segment);
			var hasRightSegment = TrafficPriority.HasRightSegment(Segment, Node) && TrafficPriority.HasRightLane(Node, Segment);

			if (CurrentMode == Mode.Simple) {
				if (!hasLeftSegment) {
					CurrentMode = Mode.SingleLeft;
				} else {
					CurrentMode = Mode.SingleRight;
				}
			} else if (CurrentMode == Mode.SingleRight) {
				if (!hasForwardSegment || !hasRightSegment) {
					CurrentMode = Mode.Simple;
				} else {
					CurrentMode = Mode.SingleLeft;
				}
			} else if (CurrentMode == Mode.SingleLeft) {
				if (!hasLeftSegment) {
					CurrentMode = Mode.Simple;
				} else {
					CurrentMode = Mode.All;
				}
			} else {
				CurrentMode = Mode.Simple;
			}

			if (CurrentMode == Mode.Simple) {
				LightLeft = LightMain;
				LightRight = LightMain;
				LightPedestrian = _checkPedestrianLight();
			}
		}

		public void ManualPedestrian() {
			PedestrianEnabled = !PedestrianEnabled;
		}

		public void ChangeLightMain() {
			var invertedLight = LightMain == RoadBaseAI.TrafficLightState.Green
				? RoadBaseAI.TrafficLightState.Red
				: RoadBaseAI.TrafficLightState.Green;

			if (CurrentMode == Mode.Simple) {
				LightLeft = invertedLight;
				LightRight = invertedLight;
				LightPedestrian = !PedestrianEnabled ? LightMain : LightPedestrian;
				LightMain = invertedLight;
			} else if (CurrentMode == Mode.SingleRight) {
				LightRight = invertedLight;
				LightMain = invertedLight;
			} else if (CurrentMode == Mode.SingleLeft) {
				LightLeft = invertedLight;
				LightMain = invertedLight;
			} else {
				LightMain = invertedLight;
			}

			if (!PedestrianEnabled) {
				LightPedestrian = _checkPedestrianLight();
			}

			UpdateVisuals();
		}

		public void ChangeLightLeft() {
			var invertedLight = LightLeft == RoadBaseAI.TrafficLightState.Green
				? RoadBaseAI.TrafficLightState.Red
				: RoadBaseAI.TrafficLightState.Green;

			LightLeft = invertedLight;

			if (!PedestrianEnabled) {
				LightPedestrian = _checkPedestrianLight();
			}

			UpdateVisuals();
		}

		public void ChangeLightRight() {
			var invertedLight = LightRight == RoadBaseAI.TrafficLightState.Green
				? RoadBaseAI.TrafficLightState.Red
				: RoadBaseAI.TrafficLightState.Green;

			LightRight = invertedLight;

			if (!PedestrianEnabled) {
				LightPedestrian = _checkPedestrianLight();
			}

			UpdateVisuals();
		}

		public bool isAnyGreen() {
			return LightMain == RoadBaseAI.TrafficLightState.Green ||
				LightLeft == RoadBaseAI.TrafficLightState.Green ||
				LightRight == RoadBaseAI.TrafficLightState.Green;
		}

		public bool isLeftGreen() {
			return LightLeft == RoadBaseAI.TrafficLightState.Green;
		}

		public bool isForwardGreen() {
			return LightMain == RoadBaseAI.TrafficLightState.Green;
		}

		public bool isRightGreen() {
			return LightRight == RoadBaseAI.TrafficLightState.Green;
		}

		public void ChangeLightPedestrian() {
			if (PedestrianEnabled) {
				var invertedLight = LightPedestrian == RoadBaseAI.TrafficLightState.Green
					? RoadBaseAI.TrafficLightState.Red
					: RoadBaseAI.TrafficLightState.Green;

				LightPedestrian = invertedLight;
				UpdateVisuals();
			}
		}

		public void UpdateVisuals() {
			var instance = Singleton<NetManager>.instance;
			var currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;

			LastChange = 0u;
			LastChangeFrame = currentFrameIndex >> 6;

			RoadBaseAI.TrafficLightState vehicleLightState;
			RoadBaseAI.TrafficLightState pedestrianLightState;
			bool vehicles;
			bool pedestrians;
			RoadBaseAI.GetTrafficLightState(Node, ref instance.m_segments.m_buffer[Segment],
				currentFrameIndex - 256u, out vehicleLightState, out pedestrianLightState, out vehicles, out pedestrians);

			if (LightMain == RoadBaseAI.TrafficLightState.Red &&
				LightLeft == RoadBaseAI.TrafficLightState.Red &&
				LightRight == RoadBaseAI.TrafficLightState.Red) {
				//Debug.Log("Changing Light with segment Id" + Segment + " and node id " + Node + " to red.");
				vehicleLightState = RoadBaseAI.TrafficLightState.Red;
			} else {
				//Debug.Log("Changing Light with segment Id" + Segment + " and node id " + Node + " to green.");
				vehicleLightState = RoadBaseAI.TrafficLightState.Green;
			}

			pedestrianLightState = LightPedestrian;

			RoadBaseAI.SetTrafficLightState(Node, ref instance.m_segments.m_buffer[Segment], currentFrameIndex,
				vehicleLightState, pedestrianLightState, vehicles, pedestrians);
		}

		private RoadBaseAI.TrafficLightState _checkPedestrianLight() {
			if (LightLeft == RoadBaseAI.TrafficLightState.Red && LightMain == RoadBaseAI.TrafficLightState.Red &&
				LightRight == RoadBaseAI.TrafficLightState.Red) {
				return RoadBaseAI.TrafficLightState.Green;
			}
			return RoadBaseAI.TrafficLightState.Red;
		}

		public object Clone() {
			return MemberwiseClone();
		}
	}
}