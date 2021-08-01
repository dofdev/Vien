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
  public List<Vector3> trees = new List<Vector3>();
  public List<Enemy> enemies = new List<Enemy>();
  public Render render;

  public Vector3 oriel;
  public Vector3 cursor;
  public float safeRadius = 0.2f;
  public float enemyRadius = 0.02f;


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
        if (enemies[i].Hit(player))
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
public class Detect
{
  public Vector3 pos;
  public float radius;

  public bool Hit(Detect other)
  {
    return Vector3.Distance(pos, other.pos) <= radius + other.radius;
  }

  // Bounds bounds = new Bounds(Vector3.zero, Vector3.one * scale * 0.5f);
  // bounds.Intersects(bounds);
}

[Serializable]
public class Player : Detect
{
  Monolith mono;

  public Vector3 dir;
  public float followDist = 0.06f;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    pos = Vector3.zero;
    dir = Vector3.back;
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

    dir = (mono.cursor - pos).normalized;
  }
}

[Serializable]
public class Gem : Detect
{
  Monolith mono;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    Spawn();
  }

  public void Spawn()
  {
    pos = Vector3.zero;
    while (pos.magnitude < mono.safeRadius || Hit(mono.player))
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
    if (!held && Hit(mono.player))
    {
      held = true;
    }
    if (held)
    {
      pos = mono.player.pos;

      if (pos.magnitude < mono.safeRadius)
      {
        mono.trees.Add(pos);

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
public class Enemy : Detect
{
  Monolith mono;

  public Vector3 dir;

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
    radius = mono.enemyRadius;

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
  public float scale = 1.0f;

  public void Start(Monolith mono)
  {
    this.mono = mono;
  }

  public List<InputDevice> devices = new List<InputDevice>();
  public void Update()
  {
    Vector3 rigPos = Vector3.zero;
    Quaternion rigRot = Quaternion.identity;

    XRHMD hmd = InputSystem.GetDevice<XRHMD>();
    if (hmd != null)
    {
      Vector3 headPos = hmd.centerEyePosition.ReadValue() * 2;
      Quaternion headRot = hmd.centerEyeRotation.ReadValue();

      rigPos = -headPos + (headRot * offset);
      // rigRot = headRot;

      cam.transform.position = Pivot(headPos, rigPos, rigRot);
      cam.transform.rotation = rigRot * headRot;
      cam.transform.localScale = Vector3.one * scale;
    }

    XRController lCon = XRController.leftHand;
    if (lCon != null)
    {
      lHand.pos = Pivot(lCon.devicePosition.ReadValue() * scale, rigPos, rigRot);
      lHand.rot = rigRot * lCon.deviceRotation.ReadValue();

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
      rHand.pos = Pivot(rCon.devicePosition.ReadValue() * scale, rigPos, rigRot);
      rHand.rot = rigRot * rCon.deviceRotation.ReadValue();

      rHand.button.Set(rCon.TryGetChildControl("primarybutton").IsPressed());
    }

    if (hmd != null)
    {
      // stretch cursor
      float stretch = Vector3.Distance(lHand.pos, rHand.pos);
      mono.cursor = rHand.pos + rHand.rot * Quaternion.Euler(45, 0, 0) * Vector3.forward * stretch * 3;
    }
  }

  public Vector3 Pivot(Vector3 pos, Vector3 pivot, Quaternion rot)
  {
    Vector3 dir = pos - pivot;
    dir = rot * dir;
    pos = dir + pivot;
    return pivot + pos;
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

  public Material matDefault, matOriel, matDebug;
  public Mesh meshCube, meshSphere, meshOriel, meshWorld, meshGem, meshTree, meshPlayer, meshEnemy;

  public void Start(Monolith mono)
  {
    this.mono = mono;
  }

  public void Update()
  {
    DrawMesh(meshCube, matDefault, mono.rig.lHand.pos, mono.rig.lHand.rot, 0.03f);
    DrawMesh(meshCube, matDefault, mono.rig.rHand.pos, mono.rig.rHand.rot, 0.03f);

    m4.SetTRS(Vector3.zero, Quaternion.identity, mono.oriel);
    Graphics.DrawMesh(meshOriel, m4, matOriel, 0);

    DrawMesh(meshWorld, matDefault, Vector3.zero, Quaternion.identity, 0.01f);

    DrawMesh(meshCube, matDefault, mono.cursor, Quaternion.identity, 0.02f);

    DrawMesh(meshPlayer, matDefault, mono.player.pos, Quaternion.LookRotation(mono.player.dir), 0.02f);

    DrawMesh(meshGem, matDefault, mono.gem.pos, Quaternion.identity, 0.02f);

    for (int i = 0; i < mono.trees.Count; i++)
    {
      DrawMesh(meshTree, matDefault, 
        mono.trees[i], Quaternion.LookRotation(mono.trees[i]), 0.004f);
    }

    for (int i = 0; i < mono.enemies.Count; i++)
    {
      DrawMesh(meshEnemy, matDefault,
        mono.enemies[i].pos, Quaternion.LookRotation(mono.enemies[i].dir), 0.005f);
    }

    if (true)
    {
      DrawMesh(meshSphere, matDebug, Vector3.zero, Quaternion.identity, mono.safeRadius / 2);
      DrawMesh(meshSphere, matDebug, mono.cursor, Quaternion.identity, mono.player.followDist / 2);
      DrawMesh(meshSphere, matDebug, mono.player.pos, Quaternion.identity, mono.player.radius / 2);
      DrawMesh(meshSphere, matDebug, mono.gem.pos, Quaternion.identity, mono.gem.radius / 2);
      for (int i = 0; i < mono.enemies.Count; i++)
      {
        DrawMesh(meshSphere, matDebug, mono.enemies[i].pos, Quaternion.identity, mono.enemies[i].radius / 2);
      }
    }
  }
  Matrix4x4 m4 = new Matrix4x4();

  void DrawMesh(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, float scale)
  {
    m4.SetTRS(pos, rot.normalized, Vector3.one * scale);
    Graphics.DrawMesh(mesh, m4, mat, 0);
  }
}