using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.AI;

  [RequireComponent(typeof(NavMeshAgent))]
  public class NpcPatrol : MonoBehaviour
  {
      [SerializeField] private Transform pointsRoot;
      [SerializeField] private float waitAtPoint = 2f;
      [SerializeField] private float arriveDistance = 0.3f;
      [SerializeField] private bool pingPong = true;

      private NavMeshAgent agent;
      private readonly List<Transform> points = new();
      private int currentIndex;
      private int direction = 1;
      private float waitTimer;
      private bool movementEnabled = true;

      private void Awake()
      {
          agent = GetComponent<NavMeshAgent>();
          BuildPoints();
      }

      private void Start()
      {
          MoveToCurrentPoint();
      }

      private void Update()
      {
          if (!movementEnabled)
          {
              agent.isStopped = true;
              return;
          }

          if (points.Count == 0 || agent.pathPending)
              return;

          float stopDistance = Mathf.Max(arriveDistance, agent.stoppingDistance);

          if (agent.hasPath && agent.remainingDistance > stopDistance)
              return;

          agent.isStopped = true;
          waitTimer += Time.deltaTime;

          if (waitTimer < waitAtPoint)
              return;

          waitTimer = 0f;
          AdvanceIndex();
          MoveToCurrentPoint();
      }

      private void BuildPoints()
      {
          points.Clear();

          if (pointsRoot == null)
              return;

          if (pointsRoot.childCount == 0)
          {
              points.Add(pointsRoot);
              return;
          }

          for (int i = 0; i < pointsRoot.childCount; i++)
          {
              Transform child = pointsRoot.GetChild(i);
              if (child != null)
                  points.Add(child);
          }
      }

      public void SetMovementEnabled(bool enabled)
      {
          movementEnabled = enabled;
          waitTimer = 0f;

          if (!enabled)
          {
              agent.isStopped = true;
              agent.ResetPath();
              return;
          }

          MoveToCurrentPoint();
      }

      private void MoveToCurrentPoint()
      {
          if (points.Count == 0)
              return;

          if (NavMesh.SamplePosition(points[currentIndex].position, out NavMeshHit hit,
  2f, NavMesh.AllAreas))
          {
              agent.isStopped = false;
              agent.SetDestination(hit.position);
          }
      }

      private void AdvanceIndex()
      {
          if (points.Count <= 1)
              return;

          if (pingPong)
          {
              if (currentIndex == points.Count - 1)
                  direction = -1;
              else if (currentIndex == 0)
                  direction = 1;

              currentIndex += direction;
          }
          else
          {
              currentIndex = (currentIndex + 1) % points.Count;
          }
      }
  }