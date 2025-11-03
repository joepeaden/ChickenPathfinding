using UnityEngine;
using System.Collections.Generic;

namespace ChickenPathfinding
{
    public class PathFollower : MonoBehaviour
    {
        Grid grid;
        List<Vector3> path;
        public float speed = 5f;
        int targetIndex;
        public Transform player;
        public Transform target;

        void Start()
        {
            grid = FindObjectOfType<Grid>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Vector3 targetPos = target.transform.position;
                targetPos.z = 0f;

                path = AStar.FindPath(grid, transform.position, targetPos);

                if (path != null && path.Count > 0)
                {
                    targetIndex = 0;
                    StopCoroutine("FollowPath");
                    StartCoroutine("FollowPath");
                }
            }
        }

        System.Collections.IEnumerator FollowPath()
        {
            Vector3 currentWaypoint = path[0];

            while (true)
            {
                if (transform.position == currentWaypoint)
                {
                    targetIndex++;
                    if (targetIndex >= path.Count)
                    {
                        yield break;
                    }
                    currentWaypoint = path[targetIndex];
                }

                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);
                yield return null;
            }
        }

        void OnDrawGizmos()
        {
            if (path != null)
            {
                for (int i = targetIndex; i < path.Count; i++)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(path[i], Vector3.one * 0.3f);

                    if (i == targetIndex)
                    {
                        Gizmos.DrawLine(transform.position, path[i]);
                    }
                    else
                    {
                        Gizmos.DrawLine(path[i - 1], path[i]);
                    }
                }
            }
        }
    }
}