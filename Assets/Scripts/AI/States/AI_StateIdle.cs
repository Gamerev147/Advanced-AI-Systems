using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_StateIdle : AI_State
{
    float idleDuration;

    public void Enter(AI_Agent agent, float arg = 0f)
    {
        Debug.Log("Entered idle state");

        agent.StateTimer = 0f;
        agent.NavAgent.isStopped = true;
        float newIdleTime = Random.Range(agent.MinIdleDuration, agent.MaxIdleDuration);

        // Check for custom idle time
        if (arg <= 0f)
        {
            idleDuration = newIdleTime;
        } else
        {
            Debug.Log("Using custom idle time");
            idleDuration = arg;
        }
    }

    public void Exit(AI_Agent agent)
    {
        
    }

    public StateID GetID()
    {
        return StateID.Idle;
    }

    public void Update(AI_Agent agent)
    {
        agent.StateTimer += Time.deltaTime;

        // Switch back to patrol or keep idling
        if (agent.StateTimer >= idleDuration)
        {
            if (agent.CanPatrol)
            {
                agent.StateMachine.ChangeState(StateID.Patrol);
            }
            else
            {
                agent.StateMachine.ChangeState(StateID.Idle);
            }
        }
    }
}
