using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_StatePatrol : AI_State
{
    public void Enter(AI_Agent agent, float arg = 0f)
    {
        Debug.Log("Entered patrol state");

        agent.NavAgent.isStopped = false;
        if (agent.PatrolPoints.Count > 0)
        {
            Vector3 pos = agent.PatrolPoints[agent.CurrentPatrolIndex].Position;
            agent.NavAgent.SetDestination(pos);
        }
    }

    public void Exit(AI_Agent agent)
    {
        
    }

    public StateID GetID()
    {
        return StateID.Patrol;
    }

    public void Update(AI_Agent agent)
    {
        // Check if player is blocking path
        if (agent.PlayerInFront() && Vector3.Distance(agent.transform.position, agent.Player.transform.position) <= 2f)
        {
            agent.StateMachine.ChangeState(StateID.Idle);
            return;
        }

        if (agent.NavAgent.pathPending || agent.NavAgent.remainingDistance > 0.5f) return;

        // Skip idling at patrol point
        if (agent.PatrolPoints[agent.CurrentPatrolIndex].SkipIdle)
        {
            UpdatePatrolIndex(agent);
            agent.StateMachine.ChangeState(StateID.Patrol);
            return;
        }
        else
        {
            // Custom idle time at each patrol point
            if (agent.PatrolPoints[agent.CurrentPatrolIndex].CustomWaitTime > 0f)
            {
                agent.StateMachine.ChangeState(StateID.Idle, agent.PatrolPoints[agent.CurrentPatrolIndex].CustomWaitTime);
                UpdatePatrolIndex(agent);
                return;
            }
            else
            {
                // Use standard idle time if custom is null
                UpdatePatrolIndex(agent);
                agent.StateMachine.ChangeState(StateID.Idle);
            }
        }
    }

    private void UpdatePatrolIndex(AI_Agent agent)
    {
        if (agent.LoopPatrol)
        {
            agent.CurrentPatrolIndex = (agent.CurrentPatrolIndex + 1) % agent.PatrolPoints.Count;
        }
        else
        {
            agent.CurrentPatrolIndex += agent.PatrolDirection;
            if (agent.CurrentPatrolIndex >= agent.PatrolPoints.Count)
            {
                agent.CurrentPatrolIndex = agent.PatrolPoints.Count - 2;
                agent.PatrolDirection = -1;
            }
            else if (agent.PatrolDirection < 0)
            {
                agent.CurrentPatrolIndex = 0;
                agent.PatrolDirection = 1;
            }
        }
    }
}
