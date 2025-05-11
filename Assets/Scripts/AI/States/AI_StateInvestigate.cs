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

        // Get player's current position
        targetPosition = agent.Player.transform.position;

        // Stop agent and grab holster
        agent.NavAgent.isStopped = true;
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
        if (!finishedPausing)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseDuration)
            {
                Debug.Log("Moving to last known player position");
                agent.NavAgent.isStopped = false;
                agent.NavAgent.SetDestination(targetPosition);
                finishedPausing = true;
            }
        }

        if (finishedPausing)
        {
            if (!agent.NavAgent.pathPending && agent.NavAgent.remainingDistance <= 0.1f)
            {
                if (agent.PlayerThreatLevel > 45f)
                {
                    //fixme
                }
            }
        }
    }
}
