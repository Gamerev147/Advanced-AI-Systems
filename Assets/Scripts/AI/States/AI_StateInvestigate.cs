using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_StateInvestigate : AI_State
{
    private float pauseDuration = 3f;
    private float pauseTimer;
    private bool finishedPausing;
    private Vector3 targetPosition;

    public void Enter(AI_Agent agent, float arg = 0f)
    {
        Debug.Log("Entered awareness state");

        // Reset pause timer
        finishedPausing = false;
        pauseTimer = 0f;
        if (arg <= 0f)
        {
            pauseDuration = 3f;
        } else
        {
            pauseDuration = arg;
        }

        // Move to the player's last known position and pause
        targetPosition = agent.Player.transform.position;
        agent.NavAgent.SetDestination(targetPosition);

        agent.NavAgent.isStopped = false;
        agent.HandHolsterRig.weight = 1f;
    }

    public void Exit(AI_Agent agent)
    {
        agent.HandHolsterRig.weight = 0f;
    }

    public StateID GetID()
    {
        return StateID.Investigate;
    }

    public void Update(AI_Agent agent)
    {
        // Agent finished moving to last know position
        if (!agent.NavAgent.pathPending && agent.NavAgent.remainingDistance <= 0.1f)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseDuration)
            {
                // Player is not a threat, return to idle
                if (agent.PlayerThreatLevel <= 45f)
                {
                    agent.ChangeAwarenessState(AwarenessState.Suspicious);
                    agent.StateMachine.ChangeState(StateID.Patrol);
                }
            }
        }
    }
}
