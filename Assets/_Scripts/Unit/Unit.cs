using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;


public enum UnitState
{
  Idling,
  Moving,
  OutOfFuel
}

public sealed class Unit : MonoBehaviour
{
  [Header("Config")]
  [SerializeField] private UnitStats stats;

  [Header("Runtime")]
  [SerializeField] private UnitState state = UnitState.Idling;
  [SerializeField] private float currentFuel;

  private NavMeshAgent agent;
  private readonly Queue<Vector3> waypoints = new Queue<Vector3>();

  private UnitPathVisualizer pathVisualizer;

  public event Action<UnitState> OnStateChanged;

  public UnitStats Stats => stats;
  public UnitState State => state;
  public float CurrentFuel => currentFuel;

  public bool IsIdle => state == UnitState.Idling;
  public bool IsOutOfFuel => state == UnitState.OutOfFuel;

  void Awake()
  {
    agent = GetComponent<NavMeshAgent>();
    if (!agent)
    {
      Debug.LogError($"{name}'s NavmeshAgent is missing.");
    }

    pathVisualizer = GetComponentInChildren<UnitPathVisualizer>();
    currentFuel = stats != null ? stats.maxFuel : 0f;
  }

  void Start()
  {
    if (stats != null) agent.speed = stats.baseSpeed;
  }

  private void Update()
  {
    if (state == UnitState.Moving && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
    {
      TryBeginNextWaypoint();
    }
  }


  // --- Public API -------------------------

  public void ClearOrders()
  {
    waypoints.Clear();
    if (agent != null) agent.ResetPath();
    SetState(UnitState.Idling);
  }

  public void QueueMove(Vector3 worldPos, bool clearQueue = false)
  {
    if (clearQueue) waypoints.Clear();
    waypoints.Enqueue(worldPos);

    if (state != UnitState.Moving) TryBeginNextWaypoint();
  }

  // --- Internal helpers -------------------

  private void TryBeginNextWaypoint()
  {
    if (waypoints.Count == 0 || IsOutOfFuel)
    {
      SetState(IsOutOfFuel ? UnitState.OutOfFuel : UnitState.Idling);
      return;
    }

    Vector3 target = waypoints.Dequeue();
    bool ok = agent.SetDestination(target);
    if (!ok)
    {
      //Failed to find a path-skip and try the next
      TryBeginNextWaypoint();
    }
    pathVisualizer.SetOrigin(transform.position);
    pathVisualizer.DrawPath(agent.path);
    Debug.Log($"{name} is moving to {agent.path}");
    SetState(UnitState.Moving);
  }

  private void SetState(UnitState next)
  {
    if (state == next) return;
    state = next;
    OnStateChanged?.Invoke(state);
  }
}

#if UNITY_EDITOR

[CustomEditor(typeof(Unit))]
public class UnitCustomInspector : Editor
{
  private SerializedProperty unitState;
  private void OnEnable()
  {
    unitState = serializedObject.FindProperty("state");
  }

  public override void OnInspectorGUI()
  {
    base.OnInspectorGUI(); //Handles public and serialized fields 
    EditorGUILayout.LabelField("Yoloswag");

    //UnitState steet = unitState.state;

    Unit unit = (Unit)target;

    if (GUILayout.Button("Test Move", GUILayout.Width(90f)))
    {
      Debug.Log("hokay");
      float walkRadius = UnityEngine.Random.Range(100, 1000);

      Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * walkRadius;

      randomDirection += unit.transform.position;

      NavMeshHit hit;

      NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1);

      Vector3 finalPosition = hit.position;

      unit.QueueMove(finalPosition, true);
    }
  }
}

#endif