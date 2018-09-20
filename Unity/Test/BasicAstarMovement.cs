using UnityEngine;
using Pathfinding;
using System.Collections.Generic;
using System;

namespace UGPXFramework.Unity {
  public class PawnPositionTargetMB : MonoBehaviour {
    public int NodesToCheckAheadForBlocking = 10;
    public float Speed = 5f;
    public float CheckForBlockingPathRefresh = 1;
    public float TimeTillNextBlockingCheck = -.1f;

    [Header("Internally Managed")]
    public bool CalculatePath = false;
    public Vector3? MoveTo = null;
    public Vector3? NextPosition = null;
    public List<GraphNode> CachedNodes;
    public GridGraph GridGraph;
    public ABPath CurrentPath;

    // @todo not sure where this should live
    GameObject PathRendererGO;

    public void Awake() {
      // @todo should probably not sure name searching
      PathRendererGO = GameObject.Find("PathRenderer");
      GridGraph = AstarPath.active.data.gridGraph;
    }

    public void Update() {
      if (CurrentPath != null) {
        if (CurrentPath.CompleteState != PathCompleteState.Complete) {
          return;
        }

        // @todo check and handle for CurrentPath.error

        StorePathNodes();
      }

      if (CalculatePath) {
        UpdateNextPosition();

        CalculatePath = false;
        TimeTillNextBlockingCheck = CheckForBlockingPathRefresh;
      }

      if (NextPosition == null) {
        TimeTillNextBlockingCheck = CheckForBlockingPathRefresh;

        return;
      }

      if (TimeTillNextBlockingCheck < 0) {
        TimeTillNextBlockingCheck = CheckForBlockingPathRefresh;

        if (HasBlockingNodeAhead()) {
          UpdateNextPosition();
        }
      }

      transform.position = Vector3.MoveTowards(transform.position, (Vector3)NextPosition, Time.deltaTime * Speed);

      if (transform.position == (Vector3)NextPosition) {
        if (CachedNodes.Count == 0) {
          NextPosition = null;
          MoveTo = null;
        } else {
          NextPosition = (Vector3?)CachedNodes[0].position;

          CachedNodes.RemoveAt(0);
        }
      }

      TimeTillNextBlockingCheck -= Time.deltaTime;
    }

    public void UpdateNextPosition() {
      if (MoveTo == null) {
        NextPosition = null;

        return;
      }

      CurrentPath = ABPath.Construct(transform.position, (Vector3)MoveTo);

      AstarPath.StartPath(CurrentPath);

      if (CurrentPath.error) {
        Debug.LogFormat("No path was found: {0}", CurrentPath.errorLog);

        NextPosition = null;
        CurrentPath = null;

        return;
      }
    }

    public void StorePathNodes() {
      if (CurrentPath == null) {
        Debug.LogFormat("could not store path nodes since there is no current path to get the nodes from");

        return;
      }

      CachedNodes = CurrentPath.path;

      // this makes sure that the pawn fully moves to the selected location and that the path does not change as to
      // the next path mid way which can cause the pawn to wiggle back and forth as it move to the next location
      if (NextPosition != null && transform.position != NextPosition) {
        return;
      }

      // the first path is where the actor currently is which can be safely ignored
      if (CachedNodes.Count > 0) {
        CachedNodes.RemoveAt(0);
      }

      NextPosition = CachedNodes.Count == 0 ? null : (Vector3?)CachedNodes[0].position;
    }

    public void SetMoveTo(Vector3 moveTo) {
      MoveTo = moveTo;
      CalculatePath = true;
    }

    public bool HasBlockingNodeAhead() {
      for (int i = 0; i < NodesToCheckAheadForBlocking && i < CachedNodes.Count; i++) {
        if (!CachedNodes[i].Walkable)
          return true;
      }

      return false;
    }
  }
}
