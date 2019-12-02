﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Valve.VR;

public class MagnifyingRect : MonoBehaviour
{
    [SerializeField]
    private GameObject _rectObject;

    [SerializeField]
    private float _rectHeight = 0.2f;

    [SerializeField]
    private float _cameraDistance = 0f;

    [SerializeField]
    private float _focalLength = 0.25f;

    [SerializeField]
    private float _imageDistance = 0.25f;

    [SerializeField]
    private float _offsetFromHands = 0.1f;

    [SerializeField]
    private float _nearPoint = 0.25f;

    [SerializeField]
    private Camera _magnifyingCamera;

    [SerializeField]
    private GameObject _debugCanvas;

    

    private Transform _playerTransform;

    private Transform _leftHand;

    private Transform _rightHand;

    private Text _debugText;

    private bool _isActive = false;

    private float _zoomDistance = 0f;

    private float _standardFov;

    private float _magnification = 1f;

    private float _realImageDistance;

    private float _eyeDistance;

    private bool _debugTextIsRed = false;

    private void Awake()
    {
        Camera mainCamera = Camera.main;
        _playerTransform = mainCamera.transform;
        _standardFov = _magnifyingCamera.fieldOfView;

        _debugText = _debugCanvas.GetComponentInChildren<Text>();
        _zoomDistance = _cameraDistance;
        ToggleMagnification(false);
    }

    private bool AreHandsAlive()
    {
        return _leftHand && _rightHand;
    }

    private void ToggleMagnification(bool isEnabled)
    {
        _rectObject.SetActive(isEnabled);
        _debugCanvas.gameObject.SetActive(isEnabled);
        _isActive = isEnabled;
    }

    public void OnHandConnectionChange(SteamVR_Behaviour_Pose pose, SteamVR_Input_Sources changedSource, bool isConnected)
    {
        if (isConnected)
        {
            pose.GetComponentInChildren<Renderer>().enabled = true;
            if (changedSource == SteamVR_Input_Sources.LeftHand)
            {
                _leftHand = pose.transform;
            } else if (changedSource == SteamVR_Input_Sources.RightHand)
            {
                _rightHand = pose.transform;
            }
        } else
        {
            if (changedSource == SteamVR_Input_Sources.LeftHand)
            {
                _leftHand = null;
            } else if (changedSource == SteamVR_Input_Sources.RightHand)
            {
                _rightHand = null;
            }
            pose.GetComponentInChildren<Renderer>().enabled = false;
            ToggleMagnification(false);
        }

    } 

    private void Update()
    {
        // If button pressed, enable the rect and move the camera
        if (SteamVR_Actions.default_GrabPinch[SteamVR_Input_Sources.Any].stateDown && AreHandsAlive())
        {
            ToggleMagnification(!_isActive);
        }

        if (_isActive)
        {
            UpdateRectDimensions();
            UpdateCameraTransform();
            UpdateDebugText();
        }
    }

    private void UpdateRectDimensions()
    {
        Transform leftTrans = _leftHand.transform;
        Transform rightTrans = _rightHand.transform;
        float width = Vector3.Distance(leftTrans.position, rightTrans.position) - _offsetFromHands;

        _rectObject.transform.localScale = new Vector3(width, _rectHeight, 1f);
        _magnifyingCamera.aspect = width / _rectHeight;
        _rectObject.transform.position = (leftTrans.position + rightTrans.position) / 2f;

        Vector3 upDir = leftTrans.forward + rightTrans.forward;
        Vector3 rightDir = rightTrans.position - leftTrans.position;
        if (Vector3.Angle(upDir, rightDir) < 1f)
        {
            ToggleMagnification(false);
            return;
        }

        Vector3 normalDir = Vector3.Cross(rightDir, upDir);
        _rectObject.transform.rotation = Quaternion.LookRotation(normalDir, upDir);
    }

    private void UpdateCameraTransform()
    {
        ISteamVR_Action_Vector2 rightHandTouch = SteamVR_Actions.default_TouchPad[SteamVR_Input_Sources.RightHand];
        if (rightHandTouch.axis.y != 0f && rightHandTouch.lastAxis.y != 0f)
        {
            _imageDistance += rightHandTouch.delta.y;
        }
        ISteamVR_Action_Vector2 leftHandTouch = SteamVR_Actions.default_TouchPad[SteamVR_Input_Sources.LeftHand];
        if (leftHandTouch.axis.y != 0f && leftHandTouch.lastAxis.y != 0f)
        {
            _focalLength += leftHandTouch.delta.y;
        }

        _eyeDistance = Vector3.Distance(_playerTransform.position, _rectObject.transform.position);
        _realImageDistance = Mathf.Abs(_eyeDistance - _imageDistance);

        _magnification = (_imageDistance / (_eyeDistance)) * (1 + (_eyeDistance / _focalLength));

        if (_magnification < 1f)
        {
            _debugTextIsRed = true;
            //_magnification = 1f;
        }
        else
        {
            _debugTextIsRed = false;
        }

        _magnifyingCamera.fieldOfView = _standardFov / Mathf.Clamp(_magnification, 1f, 10f);

        // Base zoom direction on head movement? Below is a bit nauseating
        // _magnifyingCamera.transform.position = _rectObject.transform.position + _playerTransform.forward * (_cameraDistance + _zoomAmount);

        _magnifyingCamera.transform.position = _rectObject.transform.position + _rectObject.transform.forward * _zoomDistance;
        _magnifyingCamera.transform.rotation = Quaternion.LookRotation(_playerTransform.forward);
    }

    private void UpdateDebugText()
    {
        _debugCanvas.transform.position = _rectObject.transform.position;
        _debugCanvas.transform.rotation = Quaternion.LookRotation(_playerTransform.forward, _playerTransform.up);
        if (_debugTextIsRed)
        {
            _debugText.color = Color.red;
        }
        else
        {
            _debugText.color = Color.green;
        }
        _debugText.text = "Image distance: " + _imageDistance + "\nEye distance: " + _eyeDistance + "\nFocal length: " + _focalLength + "\nMagnification: " + _magnification;
    }

}

