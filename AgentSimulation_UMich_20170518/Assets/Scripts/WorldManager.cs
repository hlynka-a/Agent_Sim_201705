using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
WorldManager.cs

Definition: 
manager that controls
- world environment
- agent variables
- menu UI
*/

public class WorldManager : MonoBehaviour {

	public List<GameObject> uiMenu_ToggleAgent = new List<GameObject> ();
	public List<GameObject> uiMenu_Speed = new List<GameObject>();
	public List<GameObject> uiMenu_Size = new List<GameObject>();
	public List<GameObject> uiMenu_Goal = new List<GameObject>();

	public List<GameObject> agentsList = new List<GameObject>();
	List<Vector3> agentsStartPosition = new List<Vector3> ();

	public List<GameObject> goalList = new List<GameObject>();

	public List<GameObject> envList = new List<GameObject>();
	public GameObject uiMenu_envList = null;

	// Use this for initialization
	void Start () {
		for (int i = 0; i < agentsList.Count; i++) {
			agentsStartPosition.Add (agentsList [i].transform.position);
		}
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void UpdateExpandMenu(bool updated){
		if (updated == true) {
			foreach (GameObject g in uiMenu_ToggleAgent){
				g.SetActive (true);
			}
			foreach (GameObject g in uiMenu_Speed){
				g.SetActive (true);
			}
			foreach (GameObject g in uiMenu_Size){
				g.SetActive (true);
			}
			foreach (GameObject g in uiMenu_Goal){
				g.SetActive (true);
			}
		} else {
			foreach (GameObject g in uiMenu_ToggleAgent){
				g.SetActive (false);
			}
			foreach (GameObject g in uiMenu_Speed){
				g.SetActive (false);
			}
			foreach (GameObject g in uiMenu_Size){
				g.SetActive (false);
			}
			foreach (GameObject g in uiMenu_Goal){
				g.SetActive (false);
			}
		}
	}

	bool pause = false;
	public void UpdateClickPause(){
		pause = !pause;

		foreach (GameObject g in agentsList) {
			AgentMove a = g.GetComponent<AgentMove> ();
			a.callUpdate = pause;
		}
	}

	public void UpdateClickReset(){
		for (int i = 0; i < agentsList.Count; i++) {
			agentsList [i].transform.position = agentsStartPosition [i];
			agentsList [i].GetComponent<AgentMove> ().state = 0;
		}
	}

	public void UpdateAgentUpdate(){
		for (int i = 0; i < agentsList.Count; i++) {
			if (uiMenu_ToggleAgent [i].GetComponent<UnityEngine.UI.Toggle> ().isOn == true) {
				agentsList [i].SetActive (true);
			} else if (uiMenu_ToggleAgent [i].GetComponent<UnityEngine.UI.Toggle> ().isOn == false) {
				agentsList [i].SetActive (false);
			}

			AgentMove a = agentsList [i].GetComponent<AgentMove> ();
			float aSpeed = 0f;
			float.TryParse(uiMenu_Speed [i].GetComponent<UnityEngine.UI.InputField> ().text, out aSpeed);
			aSpeed = Mathf.Max (aSpeed, 0f);
			aSpeed = Mathf.Min (aSpeed, 10f);
			a.speedMove = aSpeed;
			a.speedRotation = aSpeed;
			float aSize = 0f;
			float.TryParse(uiMenu_Size [i].GetComponent<UnityEngine.UI.InputField> ().text, out aSize);
			aSize = Mathf.Max (aSize, 0.1f);
			aSize = Mathf.Min (aSize, 1f);
			a.transform.localScale = new Vector3(aSize,0.5f,aSize);

			int aGoal = uiMenu_Goal [i].GetComponent<UnityEngine.UI.Dropdown> ().value;
			a.goal = goalList [aGoal].transform;
		}
	}

	public void UpdateEnvironment(){
		for (int i = 0; i < envList.Count; i++) {
			if (uiMenu_envList.GetComponent<UnityEngine.UI.Dropdown> ().value != i) {
				envList [i].SetActive (false);
			} else {
				envList [i].SetActive (true);
			}
		}

		UpdateClickReset ();
	}
}
