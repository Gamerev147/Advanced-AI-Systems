using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public enum StateID
{
    Idle,
    Patrol,
    Investigate,
    Alert,
    Attack,
    Dead
}

public interface AI_State
{
    StateID GetID();
    void Enter(AI_Agent agent, float arg = 0f);
    void Update(AI_Agent agent);
    void Exit(AI_Agent agent);
}
