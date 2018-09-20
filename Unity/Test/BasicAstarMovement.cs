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

    // @todo not sure where this should live
    GameObject PathRendererGO;

    public void Awake() {
      // @todo should probably not sure name searching
      PathRendererGO = GameObject.Find("PathRenderer");
      GridGraph = AstarPath.active.data.gridGraph;
    }

    public void Update() {
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

      ABPath path = ABPath.Construct(transform.position, (Vector3)MoveTo);

      AstarPath.StartPath(path);

      // makes the path calculation sync instead of async (why again???)
      path.BlockUntilCalculated();

      if (path.error) {
        Debug.LogFormat("No path was found ({0})", path.error);

        NextPosition = null;

        return;
      }

      CachedNodes = path.path;

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
      bool hasBlockingPath = false;
      List<GraphNode> checkNodes = CachedNodes.GetRange(0, Math.Min(CachedNodes.Count, NodesToCheckAheadForBlocking));

      foreach(var node in checkNodes) {
        Vector3 nodePosition = (Vector3)node.position;

        if (!GridGraph.GetNode((int)nodePosition.x, (int)nodePosition.y).Walkable) {
          hasBlockingPath = true;

          break;
        }
      }

      return hasBlockingPath;
    }
  }
}
