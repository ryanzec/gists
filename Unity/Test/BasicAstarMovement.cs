using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

namespace UGPXFramework.Unity {
  public class PawnPositionTargetMB : MonoBehaviour {
    [Header("Internally Managed")]
    public List<Vector3> CachedPaths;
    public Vector3? MoveTo = null;
    public Vector3? NextPosition = null;
    public float Speed = 5f;
    public float TimeTileNextUpdate = -.1f;
    public float PathRefreshRate = 2f;

    // @todo not sure where this should live
    GameObject PathRendererGO;

    public void Awake() {
      // @todo should probably not sure name searching
      PathRendererGO = GameObject.Find("PathRenderer");
    }

    public void Update() {
      if (TimeTileNextUpdate < 0) {
        UpdateNextPosition();
        TimeTileNextUpdate = PathRefreshRate;
      } else {
        TimeTileNextUpdate -= Time.deltaTime;
      }
      
      if (NextPosition != null) {
        transform.position = Vector3.MoveTowards(transform.position, (Vector3)NextPosition, Time.deltaTime * Speed);

        if (transform.position == (Vector3)NextPosition) {
          if (CachedPaths.Count == 0) {
            NextPosition = null;
          } else {
            NextPosition = CachedPaths[0];

            CachedPaths.RemoveAt(0);
          }
        }
      }
    }

    public void UpdateNextPosition() {
      if (MoveTo == null) {
        NextPosition = null;

        return;
      }

      ABPath path = ABPath.Construct(transform.position, (Vector3) MoveTo);

      AstarPath.StartPath(path);

      // makes the path calculation sync instead of async (why again???)
      path.BlockUntilCalculated();

      CachedPaths = path.vectorPath;

      // this makes sure that the pawn fully moves to the selected location and that the path does not change as to
      // the next path mid way which can cause the pawn to wiggle back and forth as it move to the next location
      if (NextPosition != null && transform.position != NextPosition) {
        return;
      }

      // the first path is where the actor currently is which can be safely ignored
      if (path.vectorPath.Count > 0) {
        CachedPaths.RemoveAt(0);
      }

      if (path.error) {
        Debug.LogFormat("No path was found ({0})", path.error);

        NextPosition = null;

        return;
      }

      NextPosition = CachedPaths.Count == 0 ? null : (Vector3?)CachedPaths[0];
    }
  }
}
