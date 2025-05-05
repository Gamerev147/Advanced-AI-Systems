using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_StateMachine
{
    public AI_State[] states;
    public AI_Agent agent;
    public StateID currentState;

    public AI_StateMachine(AI_Agent agent)
    {
        this.agent = agent;
        int numStates = System.Enum.GetNames(typeof(StateID)).Length;
        states = new AI_State[numStates];
    }

    public void RegisterState(AI_State state)
    {
        int index = (int)state.GetID();
        states[index] = state;
    }

    public AI_State GetState(StateID stateID)
    {
        int index = (int)stateID;
        return states[index];
    }

    public void Update()
    {
        GetState(currentState)?.Update(agent);
    }

    public void ChangeState(StateID newState, float arg = 0f)
    {
        GetState(currentState)?.Exit(agent);
        currentState = newState;
        GetState(currentState)?.Enter(agent, arg);
    }
}
