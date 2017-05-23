using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentMove : MonoBehaviour {

	// goal
	public Transform goal = null;

	// which movement framework are ya using?
	int moveFramework = 0;

	// which movement strategy are ya using?
	int moveStrategy = 0;

	// create path to follow towards goal

	// current state (finite state machine - see MoveToGoal())
	public int state = 0;
	// current target to move towards
	Vector3 target  = Vector3.zero;

	//printlog:
	// 0 = haven't printed any log yet, 1,2,... = printed this log last
	int printLog = 0;
	public bool showDebugRay = true;

	// Use this for initialization
	void Start () {
		
	}

	public bool callUpdate = false;

	// Update is called once per frame
	void Update () {

		if (callUpdate == false)
			return;

		if (goal == null) {
			if (printLog != 1) {
				Debug.Log (this.transform.name + " : You know, you don't have anywhere you want to go...");
				printLog = 1;
			}
			return;
		} else {
			if (printLog != 2) {
				Debug.Log (this.transform.name + " : I think I want to move towards this goal... " + goal.name);
				printLog = 2;
			}
		}

		if (Vector3.Distance (this.transform.position, goal.position) < 1f) {
			if (printLog != 3) {
				Debug.Log (this.transform.name + " : I reached my goal! " + goal.name);
				printLog = 3;
			}
		} else {
			MoveToGoal ();
		}

		if (showDebugRay == true) {
			ShowDebugRay ();
		}

	}

	void MoveToGoal(){
		/* finite state machine:
		 * 
		 * 1 = rotate directly towards goal
		 * 2 = move forward
		 * 3 = find rotation direction to determine where to move next
		 * 		(start towards goal, then check for minimum rotation required to rotate)
		 * 4 = rotate towards target rotation
		 * 
		 */

		switch(state){
		case 0:
			state = 1;
			break;
		case 1:
			target = goal.position;
			RotateToTarget ();
			break;
		case 2:
			MoveForward ();
			break;
		case 3:
			//TODO
			FindNewTarget();
			break;
		case 4:
			RotateToTarget ();
			break;
		default:
			break;
		}
	}

	Quaternion targetRotation = Quaternion.identity;
	Quaternion previousRotation = Quaternion.identity;
	float timePassed = 0f;
	public float speedRotation = 1f;
	public float speedMove = 1f;

	void RotateToTarget(){
		if (targetRotation == null || targetRotation == Quaternion.identity) {
			targetRotation = Quaternion.LookRotation (target - this.transform.position);
			previousRotation = this.transform.rotation;
			timePassed = 0;
		}

		timePassed += Time.deltaTime;

		float rotateMagnitude = timePassed * speedRotation;

		// rotate towards target, also restrict rotation not to be above
		this.transform.rotation = Quaternion.Lerp (previousRotation, targetRotation, rotateMagnitude);
		this.transform.eulerAngles = new Vector3 (0,this.transform.rotation.eulerAngles.y, 0);

		if (rotateMagnitude >= 1.0f) {
			state = 2;
			targetRotation = Quaternion.identity;
		}
	}

	int angleToRotate = 0;
	Vector3 leftPosition = Vector3.zero;
	Vector3 rightPosition = Vector3.zero;
	void FindNewTarget(){
		// start with goal, then rotate left and right to decide which will allow to move forward

		Vector3 originalTarget = goal.position;

		while (angleToRotate < 180){
			RaycastHit hitInfo;
			leftPosition = (Quaternion.Euler (new Vector3(0, -angleToRotate, 0)) * (originalTarget - this.transform.position)) 
				+ this.transform.position;
			rightPosition = (Quaternion.Euler (new Vector3 (0, angleToRotate, 0)) * (originalTarget - this.transform.position))
			    + this.transform.position;
			if (!Physics.Raycast(this.transform.position, 
				(leftPosition - this.transform.position), out hitInfo, maxDistanceBlock * 2)){
			/*if (!Physics.SphereCast (this.transform.position, 1, 
				(leftPosition - this.transform.position), out hitInfo, maxDistanceBlock * 2)) {*/
				// leftPosition is good! Move towards it in next step.
				target = leftPosition;
				angleToRotate = 0;
				rightPosition = Vector3.zero;
				state = 4;
				Debug.Log (this.transform.name + " : I will rotate left by " + angleToRotate);
				break;
			} else if (!Physics.Raycast(this.transform.position, 
				(rightPosition - this.transform.position), out hitInfo, maxDistanceBlock * 2)){
				Debug.Log (this.transform.name + " : I will rotate right by " + angleToRotate);
				target = rightPosition;
				angleToRotate = 0;
				leftPosition = Vector3.zero;
				state = 4;
				break;
			} else {
				// to see angle searching in real time
				angleToRotate++;
				target = leftPosition;
				break;
			}

		}

		//assumption: agent will not be trapped - they can at the very least move backwards.

	}

	// how far to look ahead before looking away?
	float maxDistanceBlock = 0.5f;

	void MoveForward(){

		//what if something is in the way?
		RaycastHit hitInfo;
		if (Physics.Raycast (this.transform.position, this.transform.forward, out hitInfo, maxDistanceBlock)) {
			// you are about to hit something! But what if it is a player vs a wall?
			// for now, just focus on wall logic
			state = 3;
		} else {
			// what if there is no direct obstacle between you and the goal? What if you are not already moving towards the goal? You might move past it!
			// ignore other agents for this... they are set to be on layer 8
			LayerMask ignoreAgents = 0;
			ignoreAgents = 1 << 8;
			if (target != goal.transform.position
			    && !Physics.Raycast (this.transform.position, 
						goal.position - this.transform.position, 
						Vector3.Distance (goal.position, this.transform.position),
						ignoreAgents)) {
				this.transform.rotation = Quaternion.Lerp(this.transform.rotation,Quaternion.LookRotation (goal.position - this.transform.position),0.25f);
				this.transform.eulerAngles = new Vector3 (0,this.transform.rotation.eulerAngles.y, 0);
			} 

			this.transform.Translate (Vector3.forward * speedMove * Time.deltaTime);

			//what if I'm too big and about to graze something?
			if (Physics.Raycast (this.transform.position + (this.transform.right * this.transform.lossyScale.x * 0.5f), this.transform.forward, out hitInfo, maxDistanceBlock)) {
				this.transform.Translate (-Vector3.right * speedMove * Time.deltaTime);
			} 
			if (Physics.Raycast (this.transform.position - (this.transform.right * this.transform.lossyScale.x * 0.5f), this.transform.forward, out hitInfo, maxDistanceBlock)) {
				this.transform.Translate (Vector3.right * speedMove * Time.deltaTime);
			}

		}
	}


	void ShowDebugRay(){
		Debug.DrawRay (this.transform.position, 
			(goal.position - this.transform.position).normalized * (Vector3.Distance(this.transform.position,goal.position)), 
			Color.black);
		Debug.DrawRay (this.transform.position, (this.transform.forward).normalized * maxDistanceBlock, Color.red);
		Debug.DrawRay (this.transform.position + (this.transform.right * this.transform.lossyScale.x * 0.5f), 
			(this.transform.forward).normalized * maxDistanceBlock, Color.red);
		Debug.DrawRay (this.transform.position - (this.transform.right * this.transform.lossyScale.x * 0.5f), 
			(this.transform.forward).normalized * maxDistanceBlock, Color.red);
		if (leftPosition != Vector3.zero) {
			Debug.DrawRay (this.transform.position, (leftPosition - this.transform.position).normalized * maxDistanceBlock * 2, Color.green);
		}
		if (rightPosition != Vector3.zero) {
			Debug.DrawRay (this.transform.position, (rightPosition - this.transform.position).normalized * maxDistanceBlock * 2, Color.green);
		}
	}
}
