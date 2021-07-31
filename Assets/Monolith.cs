using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

using Random = UnityEngine.Random;

public class Monolith : MonoBehaviour
{
  public Rig rig;
  public Render render;

  void Awake()
  {

  }

  void Start()
  {
    rig.Start(this);
    render.Start(this);
  }

  void Update()
  {
    rig.Update();

    render.Update();
  }
}

[Serializable]
public class Rig
{
  Monolith mono;

  public Camera cam;
  public PhysicalInput lHand, rHand;

  [SerializeField]
  public InputControl con;

  public void Start(Monolith mono)
  {
    this.mono = mono;
  }

  public List<InputDevice> devices = new List<InputDevice>();
  public void Update()
  {
    XRHMD hmd = InputSystem.GetDevice<XRHMD>();
    if (hmd != null)
    {
      cam.transform.position = hmd.centerEyePosition.ReadValue();
      cam.transform.rotation = hmd.centerEyeRotation.ReadValue();
    }

    XRController lCon = XRController.leftHand;
    if (lCon != null)
    {
      lHand.pos = lCon.devicePosition.ReadValue();
      lHand.rot = lCon.deviceRotation.ReadValue();

      lHand.button.Set(lCon.TryGetChildControl("primarybutton").IsPressed());

      // foreach (InputControl ic in lCon.children)
      // {
      //   Debug.Log(ic.name);
      // }
      // Debug.LogError("WOWWEEE");
    }

    XRController rCon = XRController.rightHand;
    if (rCon != null)
    {
      rHand.pos = rCon.devicePosition.ReadValue();
      rHand.rot = rCon.deviceRotation.ReadValue();

      rHand.button.Set(rCon.TryGetChildControl("primarybutton").IsPressed());
    }
  }
}

[Serializable]
public class PhysicalInput
{
  public Vector3 pos;
  public Quaternion rot;

  public Btn button;
}

[Serializable]
public class Btn
{
  public bool down;
  public bool held;
  public bool up;

  public void Set(bool held)
  {
    down = up = false;
    if (this.held)
    {
      if (!held)
      {
        up = true;
      }
    }
    else
    {
      if (held)
      {
        down = true;
      }
    }
    this.held = held;
  }
}


[Serializable]
public class Render
{
  Monolith mono;

  public Material matDefault;
  public Mesh meshCube;

  public void Start(Monolith mono)
  {
    this.mono = mono;
  }

  public void Update()
  {
    DrawMesh(meshCube, matDefault, mono.rig.lHand.pos, mono.rig.lHand.rot, Vector3.one * 0.06f);
    DrawMesh(meshCube, matDefault, mono.rig.rHand.pos, mono.rig.rHand.rot, Vector3.one * 0.06f);

  }
  Matrix4x4 m4 = new Matrix4x4();

  void DrawMesh(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
  {
    m4.SetTRS(pos, rot.normalized, scale); 
    Graphics.DrawMesh(mesh, m4, mat, 0);
  }
}