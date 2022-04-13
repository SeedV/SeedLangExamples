using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CoroutineActions;

public class GameManager : MonoBehaviour {
  public GameObject RobotBase;
  public GameObject Stacks;
  public GameObject ToolbarPanel;
  public GameObject EditorPanel;
  public GameObject InspectorPanel;
  public GameObject MonitorPanel;
  public GameObject StatsPanel;

  private Vector3 _robotOriginalPos;

  private ActionQueue _actionQueue = new ActionQueue();

  void Start() {
    _robotOriginalPos = RobotBase.transform.localPosition;
    _actionQueue.Enqueue(new SingleTaskAction(this, new Task2<float, float>(TestTask, 3, 3)));

    StartCoroutine(_actionQueue.RunUntilEmpty());
  }

  private IEnumerator TestTask(float newX, float newZ) {
    Debug.Log("Start Test Action");
    yield return new WaitForSeconds(1);
    RobotBase.transform.localPosition = new Vector3(newX, _robotOriginalPos.y, newZ);
    yield return new WaitForSeconds(1);
    RobotBase.transform.localPosition = _robotOriginalPos;
    Debug.Log("End of Test Action");
  }
}
