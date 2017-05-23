using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
AgentMove.cs
- Controls basic AI system for individual agent.
*/


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
			FindNewTarget();
			break;
		case 4:
			RotateToTarget ();
			break;
		case 5: 
			StartSearchingNewTarget ();
			break;
		default:
			break;
		}
	}

	// targetRotaiton/previousRotation = used to have consistent rotation speed between start and end rotation
	Quaternion targetRotation = Quaternion.identity;
	Quaternion previousRotation = Quaternion.identity;
	float timePassed = 0f;
	public float speedRotation = 1f;
	public float speedMove = 1f;

	/* RotateToTarget() :
	 - rotation angle already determined - rotate towards it at consistent speed.
	*/
	void RotateToTarget(){
		// if rotation angle hasn't actually been set yet, calculate based on target position (target is not necessarily final goal)
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

		// Quaternion.Lerp(a,b,1) => a, Quaternion.Lerp(a,b,1) => b, use this to determine if finished. 
		if (rotateMagnitude >= 1.0f) {
			state = 2;
			targetRotation = Quaternion.identity;
		}
	}

	// angleToRotate = increment to see how many angles to rotate
	int angleToRotate = 0;
	// leftPosition/rightPosition = target position to look at rotated left/right, to see if obstacles in between
	Vector3 leftPosition = Vector3.zero;
	Vector3 rightPosition = Vector3.zero;
	// targetDirectionCounter/targetDirectionMod = to determine if to reset rotation towards final goal or not
	int targetDirectionCounter = 0;
	int targetDirectionMod = 3;
	// target to reset to before rotating to find best direction
	Vector3 originalTarget = Vector3.zero;

	/* StartSearchingNewTarget():
	- Decide where to initialize agent rotation to determine best next direction to move.
	- By sometimes initializing towards final goal, helps reset agent to eventually make it towards end. 
	*/
	void StartSearchingNewTarget(){
		// start with goal, then rotate left and right to decide which will allow to move forward

		// A = nearest rotation from current rotation, B = neareest rotation from rotation to goal
		// 1 => A, 2 => A,  3 => B, 
		// 4 => A, 5 => A,  6 => A,  7 => B, 
		// 8 => A, 9 => A, 10 => A, 11 => A, 12 => B, ...
		targetDirectionCounter++;
		if (targetDirectionCounter % targetDirectionMod == 0) {
			originalTarget = goal.position;
			targetDirectionCounter++;
			targetDirectionMod++;
			if (targetDirectionMod > 7) {
				targetDirectionMod = 3;
			}
		} else {
			originalTarget = this.transform.position + (this.transform.forward*10);
		}

		state = 3;
	}

	/* FindNewTarget():
	- Find least amount to rotate to be able to move (left or right)
	*/
	void FindNewTarget(){

		// note: set to update search angle every frame, to easily trace.
		while (angleToRotate < 180){
			RaycastHit hitInfo;
			leftPosition = (Quaternion.Euler (new Vector3(0, -angleToRotate, 0)) * (originalTarget - this.transform.position)) 
				+ this.transform.position;
			rightPosition = (Quaternion.Euler (new Vector3 (0, angleToRotate, 0)) * (originalTarget - this.transform.position))
			    + this.transform.position;
			if (!Physics.Raycast(this.transform.position, 
				(leftPosition - this.transform.position), out hitInfo, maxDistanceBlock * 2)){
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
				// to calculate immediately (not real time), remove break; line here.
				break;
			}

		}
		//assumption: agent will not be trapped - they can at the very least move backwards.
	}

	// how far to look ahead before looking for a way around?
	float maxDistanceBlock = 0.5f;

	/* MoveForward():
	- agent should be rotated in correct direction, this function is to move forward.
	*/
	void MoveForward(){
		//what if something is in the way?
		RaycastHit hitInfo;
		if (Physics.Raycast (this.transform.position, this.transform.forward, out hitInfo, maxDistanceBlock)) {
			// you are about to hit something! But what if it is a player vs a wall?
			// for now, just focus on wall logic
			state = 5;
		} else {
			// what if there is no direct obstacle between you and the goal? What if you are not already moving towards the goal? You might move past it!
			// ignore other agents for this... they are set to be on layer 8
			LayerMask ignoreAgents = 0;
			ignoreAgents = 1 << 8;
			ignoreAgents = ~ignoreAgents;
			if (target != goal.transform.position
			    && !Physics.Raycast (this.transform.position, 
						goal.position - this.transform.position, 
						Vector3.Distance (goal.position, this.transform.position),
						ignoreAgents)) {
				this.transform.rotation = Quaternion.Lerp(this.transform.rotation,Quaternion.LookRotation (goal.position - this.transform.position),0.25f);
				this.transform.eulerAngles = new Vector3 (0,this.transform.rotation.eulerAngles.y, 0);
			} 

			// move forward
			this.transform.Translate (Vector3.forward * speedMove * Time.deltaTime);
			//what if I'm too big and about to graze something? Strafe to the left or right a bit...
			if (Physics.Raycast (this.transform.position + (this.transform.right * this.transform.lossyScale.x * 0.5f), this.transform.forward, out hitInfo, maxDistanceBlock)) {
				this.transform.Translate (-Vector3.right * speedMove * Time.deltaTime);
			} 
			if (Physics.Raycast (this.transform.position - (this.transform.right * this.transform.lossyScale.x * 0.5f), this.transform.forward, out hitInfo, maxDistanceBlock)) {
				this.transform.Translate (Vector3.right * speedMove * Time.deltaTime);
			}

		}
	}


	/* ShowDebugRay():
	- optional function to show ray lines while executed in Unity editor to help trace AI.
	*/
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
