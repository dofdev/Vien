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
  public Player player;
  public Gem gem;
  public List<Enemy> enemies = new List<Enemy>();
  public Render render;

  public Vector3 oriel;
  public Vector3 cursor;
  public float safeRadius = 0.2f;


  void Awake()
  {

  }

  void Start()
  {
    rig.Start(this);

    player.Start(this);
    gem.Start(this);

    render.Start(this);
  }

  bool playing = true;
  void Update()
  {
    rig.Update();

    if (playing)
    {
      player.Update();
      gem.Update();
      for (int i = 0; i < enemies.Count; i++)
      {
        enemies[i].Update();
        if (Vector3.Distance(enemies[i].pos, player.pos) < 0.02f)
        {
          Debug.Log("game over");
          // alternative ending is where all the enemies target what spawned them, and phase out
          playing = false;
        }
      }
    }



    render.Update();
  }

  public Vector3 OutOfBounds(Vector3 pos)
  {
    if (pos.x < -oriel.x / 2)
    {
      return Vector3.right;
    }
    else if (pos.x > oriel.x / 2)
    {
      return Vector3.left;
    }
    else if (pos.y < -oriel.y / 2)
    {
      return Vector3.up;
    }
    else if (pos.y > oriel.y / 2)
    {
      return Vector3.down;
    }
    else if (pos.z < -oriel.z / 2)
    {
      return Vector3.forward;
    }
    else if (pos.z > oriel.z / 2)
    {
      return Vector3.back;
    }
    return Vector3.zero;
  }
}

[Serializable]
public class Player
{
  Monolith mono;

  public Vector3 pos;
  public float followDist = 0.06f;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    pos = Vector3.zero;
  }

  public void Update()
  {
    if (Vector3.Distance(mono.cursor, pos) > followDist)
    {
      float speed = 0.5f;
      // slower inside safeRadius
      if (pos.magnitude < mono.safeRadius)
      {
        speed *= 0.5f;
      }
      Vector3 newPos = pos + (mono.cursor - pos).normalized * speed * Time.deltaTime;
      if (mono.OutOfBounds(newPos) == Vector3.zero)
      {
        pos = newPos;
      }
    }
  }
}

[Serializable]
public class Gem
{
  Monolith mono;
  public Vector3 pos;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    Spawn();
  }

  public void Spawn()
  {
    pos = Vector3.zero;
    while (pos.magnitude < mono.safeRadius)
    {
      pos = new Vector3(
        Random.Range(-mono.oriel.x / 2, mono.oriel.x / 2),
        Random.Range(-mono.oriel.y / 2, mono.oriel.y / 2),
        Random.Range(-mono.oriel.z / 2, mono.oriel.z / 2)
      );
    }
  }

  bool held = false;
  public void Update()
  {
    if (!held && Vector3.Distance(mono.player.pos, pos) < 0.04f)
    {
      held = true;
    }
    if (held)
    {
      pos = mono.player.pos;

      if (pos.magnitude < mono.safeRadius)
      {
        Enemy enemy = new Enemy();
        enemy.Start(mono);
        mono.enemies.Add(enemy);
        Spawn();

        held = false;
      }
    }
  }
}

[Serializable]
public class Enemy
{
  Monolith mono;

  public Vector3 pos, dir;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    // spawn out, then clamp in
    pos = Random.rotation * Vector3.forward * mono.oriel.x * 2;
    pos.x = Mathf.Clamp(pos.x, -mono.oriel.x / 2, mono.oriel.x / 2);
    pos.y = Mathf.Clamp(pos.y, -mono.oriel.y / 2, mono.oriel.y / 2);
    pos.z = Mathf.Clamp(pos.z, -mono.oriel.z / 2, mono.oriel.z / 2);
    dir = Random.rotation * Vector3.forward;
  }

  public void Update()
  {
    // move forward
    pos += dir * 0.25f * Time.deltaTime;
    Vector3 normal = mono.OutOfBounds(pos);
    if (normal != Vector3.zero && Vector3.Angle(normal, dir) > 90)
    {
      dir = Vector3.Reflect(dir, normal);
      // pos += rot * Vector3.forward * 0.25f * Time.deltaTime;
    }
    else
    {
      // reflect off of safeRadius
      if (pos.magnitude < mono.safeRadius)
      {
        dir = Vector3.Reflect(dir, pos.normalized);
        pos += dir * 0.25f * Time.deltaTime;
      }
    }
  }
}

[Serializable]
public class Rig
{
  Monolith mono;

  public Camera cam;
  public PhysicalInput lHand, rHand;

  public Vector3 offset;

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
      cam.transform.position = offset + hmd.centerEyePosition.ReadValue();
      cam.transform.rotation = hmd.centerEyeRotation.ReadValue();
    }

    XRController lCon = XRController.leftHand;
    if (lCon != null)
    {
      lHand.pos = offset + lCon.devicePosition.ReadValue();
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
      rHand.pos = offset + rCon.devicePosition.ReadValue();
      rHand.rot = rCon.deviceRotation.ReadValue();

      rHand.button.Set(rCon.TryGetChildControl("primarybutton").IsPressed());
    }

    mono.cursor = rHand.pos;
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
  public Mesh meshCube, meshOriel, meshWorld, meshGem, meshPlayer, meshEnemy;

  public void Start(Monolith mono)
  {
    this.mono = mono;
  }

  public void Update()
  {
    DrawMesh(meshCube, matDefault, mono.rig.lHand.pos, mono.rig.lHand.rot, 0.03f);
    DrawMesh(meshCube, matDefault, mono.rig.rHand.pos, mono.rig.rHand.rot, 0.03f);

    m4.SetTRS(Vector3.zero, Quaternion.identity, mono.oriel);
    Graphics.DrawMesh(meshOriel, m4, matDefault, 0);

    DrawMesh(meshWorld, matDefault, Vector3.zero, Quaternion.identity, mono.safeRadius);

    DrawMesh(meshCube, matDefault, mono.cursor, Quaternion.identity, 0.02f);

    DrawMesh(meshPlayer, matDefault, mono.player.pos, Quaternion.identity, 0.02f);

    DrawMesh(meshGem, matDefault, mono.gem.pos, Quaternion.identity, 0.02f);

    for (int i = 0; i < mono.enemies.Count; i++)
    {
      DrawMesh(meshEnemy, matDefault,
        mono.enemies[i].pos, Quaternion.LookRotation(mono.enemies[i].dir), 0.01f);
    }
  }
  Matrix4x4 m4 = new Matrix4x4();

  void DrawMesh(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, float scale)
  {
    m4.SetTRS(pos, rot.normalized, Vector3.one * scale);
    Graphics.DrawMesh(mesh, m4, mat, 0);
  }
}