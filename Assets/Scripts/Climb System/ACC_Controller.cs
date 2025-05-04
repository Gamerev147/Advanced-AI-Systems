using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACC_Controller : MonoBehaviour
{
    public float forwardValue, verticalValue, horizontalValue;

    private ACC_ClimbPoint currentClimbPoint;

    private EnvironmentChecker _ec;
    private PlayerClimbController _climbController;
    private ThirdPersonController _playerController;

    private void Start()
    {
        _ec = GetComponent<EnvironmentChecker>();
        _climbController = GetComponent<PlayerClimbController>();
        _playerController = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (!_playerController.PlayerHanging)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_ec.CheckClimbing(transform.forward, out RaycastHit climbInfo))
                {
                    currentClimbPoint = climbInfo.transform.GetComponent<ACC_ClimbPoint>();

                    _playerController.SetControl(false);

                    forwardValue = 0.11f;
                    verticalValue = 0.125f;
                    horizontalValue = 0f;

                    StartCoroutine(ClimbToLedge("IdleToHang", climbInfo.transform, 0.4f, 0.54f, 
                        playerHandOffset: new Vector3(forwardValue, verticalValue, horizontalValue)));
                }
            }
        } else
        {
            // Ledge to ledge jumping
            float horInput = Mathf.Round(Input.GetAxis("Horizontal"));
            float verInput = Mathf.Round(Input.GetAxis("Vertical"));
            Vector2 inputDir = new Vector2(horInput, verInput);

            if (_climbController.PlayerInAction || inputDir == Vector2.zero)
            {
                return;
            }

            var neighbor = currentClimbPoint.GetNeighbor(inputDir);
            if (neighbor != null)
            {
                if (neighbor.connectionType == ConnectionType.Jump && Input.GetKeyDown(KeyCode.Space))
                {
                    currentClimbPoint = neighbor.climbPoint;
                    if (neighbor.pointDirection.y == 1)
                    {
                        forwardValue = 0.21f;
                        verticalValue = 0.12f;
                        horizontalValue = 0f;

                        StartCoroutine(ClimbToLedge("HangHopUp", currentClimbPoint.transform, 0.32f, 0.62f, 
                            playerHandOffset: new Vector3(forwardValue, verticalValue, horizontalValue)));
                    } else if (neighbor.pointDirection.y == -1)
                    {
                        forwardValue = 0.21f;
                        verticalValue = 0.12f;
                        horizontalValue = 0f;

                        StartCoroutine(ClimbToLedge("HangDrop", currentClimbPoint.transform, 0.3f, 0.65f, 
                            playerHandOffset: new Vector3(forwardValue, verticalValue, horizontalValue)));
                    } else if (neighbor.pointDirection.x == 1)
                    {
                        StartCoroutine(ClimbToLedge("HangHopRight", currentClimbPoint.transform, 0.2f, 0.5f));
                    } else if (neighbor.pointDirection.x == -1)
                    {
                        StartCoroutine(ClimbToLedge("HangHopLeft", currentClimbPoint.transform, 0.2f, 0.5f));
                    }
                } else if (neighbor.connectionType == ConnectionType.Move)
                {
                    currentClimbPoint = neighbor.climbPoint;

                    if (neighbor.pointDirection.x == 1) // Move right
                    {
                        StartCoroutine(ClimbToLedge("ShimmyRight", currentClimbPoint.transform, 0f, 0.3f));
                    }

                    if (neighbor.pointDirection.x == -1) // Move left
                    {
                        StartCoroutine(ClimbToLedge("ShimmyLeft", currentClimbPoint.transform, 0f, 0.3f, AvatarTarget.LeftHand));
                    }
                }
            } else
            {
                Debug.Log("No neighboring point!");
            }
        }
    }

    private IEnumerator ClimbToLedge(string animationName, Transform climbPoint, float compareStartTime, float compareEndTime, 
        AvatarTarget hand = AvatarTarget.RightHand, Vector3? playerHandOffset = null)
    {
        var compareParams = new CompareTargetParams()
        {
            position = SetHandPosition(climbPoint, hand, playerHandOffset),
            bodyTarget = hand,
            positionWeight = Vector3.one,
            startTime = compareStartTime,
            endTime = compareEndTime
        };

        var requireRot = Quaternion.LookRotation(-climbPoint.forward);

        yield return _climbController.PerformAction(animationName, compareParams, requireRot, true);

        _playerController.PlayerHanging = true;
    }

    private Vector3 SetHandPosition(Transform climbPoint, AvatarTarget hand, Vector3? playerHandOffset)
    {
        var offset = (playerHandOffset != null) ? playerHandOffset.Value : new Vector3(forwardValue, verticalValue, horizontalValue);
        var handDir = (hand == AvatarTarget.RightHand) ? climbPoint.right : -climbPoint.right;

        return climbPoint.position + climbPoint.forward * forwardValue + Vector3.up * verticalValue - handDir * horizontalValue;
    }
}
