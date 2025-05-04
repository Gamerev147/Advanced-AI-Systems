using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Climb Menu/New Climb Action")]
public class ClimbActionSO : ScriptableObject
{
    [Header("Properties")]
    [SerializeField] private string animationName;
    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
    [SerializeField] private bool lookAtObstacle;

    [Header("Target Matching")]
    [SerializeField] private bool useTargetMatching = true;
    [SerializeField] private AvatarTarget compareBodyPart;
    [SerializeField] private float compareStartTime;
    [SerializeField] private float compareEndTime;
    [SerializeField] private Vector3 positionWeight = new Vector3 (0f, 1f, 0f);

    public Vector3 ComparePosition { get; set; }

    public Quaternion RequiredRotation { get; set; }
    
    public bool CheckAvailable(ObstacleInfo hitData, Transform playerTransform)
    {
        float checkHeight = hitData.heightInfo.point.y - playerTransform.position.y;

        if (checkHeight < minHeight || checkHeight > maxHeight)
        {
            return false;
        } else
        {
            if (lookAtObstacle)
            {
                RequiredRotation = Quaternion.LookRotation(-hitData.hitInfo.normal);
            }

            if (useTargetMatching)
            {
                ComparePosition = hitData.heightInfo.point;
            }

            return true;
        }
    }

    public string AnimationName => animationName;
    public bool LookAtObstacle => lookAtObstacle;

    public bool UseTargetMatching => useTargetMatching;
    public AvatarTarget CompareBodyPart => compareBodyPart;
    public float CompareStartTime => compareStartTime;
    public float CompareEndTime => compareEndTime;
    public Vector3 PositionWeight => positionWeight;
}
