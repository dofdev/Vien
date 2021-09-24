using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#if UNITY_EDITOR
using UnityEditor.Presets;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif
using TMPro;

using Random = UnityEngine.Random;

public class Monolith : MonoBehaviour
{
  public Rig rig;
  public Oriel oriel;
  public Player player;
  public Gem gem;
  public List<Tree> trees = new List<Tree>();
  public List<Enemy> enemies = new List<Enemy>();
  public Render render;
  public SFX sfx;
  public Music music;
  public ScreenCap screenCap;

  public Vector3 cursor;

  [HideInInspector]
  public bool grayscale;

  void Awake()
  {
    grayscale = false;

    oriel.Start(this);
    render.Start(this);
    rig.Start(this);
    sfx.Start(this);
    music.Start(this);
    screenCap.Start(this);
  }

  void Start()
  {
    trees.Clear();
    enemies.Clear();

    player.Start(this);
    gem.Start(this);

    oriel.textMesh.text = "START";
  }

  [HideInInspector]
  public bool playing = false;
  void Update()
  {
    rig.Update();
    oriel.Update();
    // offsetCursor.Update();

    Mouse mouse = Mouse.current;
    if (mouse != null && Mouse.current.leftButton.wasPressedThisFrame)
    {
      // Enemy enemy = new Enemy();
      // enemy.Start(this, Random.rotation * Vector3.forward * 0.01f);
      // enemies.Add(enemy);

      // rig.rHand.button.down = true;
    }


    if (!playing)
    {
      if (rig.rHand.button.down || rig.lHand.button.down)
      {
        sfx.Play("button", cursor);
        player.Stop();
        Start();
        oriel.textMesh.text = "";
        playing = true;
      }
    }
    else
    {
      player.Update();
      gem.Update();
    }

    for (int i = 0; i < enemies.Count; i++)
    {
      enemies[i].Update();
      if (playing)
      {
        bool hit = enemies[i].Hit(player);
        for (int j = 0; j < enemies[i].segments.Count; j++)
        {
          if (hit) break;

          hit = enemies[i].segments[j].Hit(player);
        }

        if (hit)
        {
          oriel.textMesh.text = trees.Count + " <br>RESET?";
          sfx.Play("gameover", player.pos);
          sfx.Play("explosion", player.pos);
          render.PlayPS("PlayerDestroyPS", player.pos, player.dir);
          player.vel = 0;
          playing = false;
        }
      }
      else
      {
        if (enemies[i].bounced)
        {
          for (int j = trees.Count - 1; j >= 0; j--)
          {
            if (enemies[i].Hit(trees[j]))
            {
              render.PlayPS("TreeDestroyPS", trees[j].pos, trees[j].pos.normalized);
              trees.RemoveAt(j);
              return;
            }
          }
        }
      }
    }

    render.Update();
    sfx.Update();
    music.Update();
    screenCap.Update();
  }
}

[Serializable]
public class Oriel
{
  Monolith mono;

  [HideInInspector] public Transform transform;
  [HideInInspector] public GameObject[] prefabs;
  [HideInInspector] public TextMeshPro textMesh;

  [HideInInspector] public Vector3 size;
  [HideInInspector] public float safeRadius;
  [HideInInspector] public float planetRadius;
  [HideInInspector] public Vector3 offset;
  [HideInInspector] public float scale;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    size = new Vector3(0.8f, 0.7f, 0.8f);
    offset = new Vector3(0, 0, 0.9f);
    scale = 0.5f;
    safeRadius = 0.15f;
    planetRadius = 0.1f;

    transform = new GameObject("Oriel").transform;
    prefabs = Resources.LoadAll<GameObject>("Prefabs/");
    for (int i = 0; i < prefabs.Length; i++)
    {
      string name = prefabs[i].name;
      prefabs[i] = GameObject.Instantiate(prefabs[i], transform);
      prefabs[i].name = name;
      // Debug.Log(prefabs[i].name);
    }

    textMesh = GetPrefab("TextMesh").GetComponent<TextMeshPro>();
    textMesh.transform.position = Vector3.back * size.z / 2;
  }

  public GameObject GetPrefab(string name)
  {
    for (int i = 0; i < prefabs.Length; i++)
    {
      if (prefabs[i].name == name)
      {
        return prefabs[i];
      }
    }
    return null;
  }

  public void Update()
  {
    Transform headset = mono.rig.headset.transform;
    transform.position = headset.position + headset.rotation * offset;
    transform.rotation = Quaternion.Euler(0, -headset.transform.rotation.eulerAngles.y * 4, 0);
    transform.localScale = Vector3.one * scale;





    // offset cursor class too
  }

  public Vector3 OutOfBounds(Vector3 pos)
  {
    if (pos.x < -size.x / 2)
    {
      return Vector3.right;
    }
    else if (pos.x > size.x / 2)
    {
      return Vector3.left;
    }
    else if (pos.y < -size.y / 2)
    {
      return Vector3.up;
    }
    else if (pos.y > size.y / 2)
    {
      return Vector3.down;
    }
    else if (pos.z < -size.z / 2)
    {
      return Vector3.forward;
    }
    else if (pos.z > size.z / 2)
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
  [HideInInspector]
  public float radius;

  public bool Hit(Detect other)
  {
    return Vector3.Distance(pos, other.pos) <= radius + other.radius;
  }
}

[Serializable]
public class Player : Detect
{
  Monolith mono;

  public Vector3 dir;
  [HideInInspector]
  public float followDist;
  [HideInInspector]
  public float speed;

  [HideInInspector]
  public float vel;

  LineRenderer trail;
  public void Start(Monolith mono)
  {
    this.mono = mono;

    pos = Vector3.zero;
    dir = Vector3.back;
    radius = 0.02f;
    followDist = 0.12f;
    speed = 0.2f;

    GameObject newObj = new GameObject("Player Trail");
    newObj.transform.parent = mono.oriel.transform;
    newObj.transform.localPosition = Vector3.zero;
    newObj.transform.localRotation = Quaternion.identity;
    trail = newObj.AddComponent<LineRenderer>();
    trail.material = mono.render.Material("Add");
    trail.positionCount = 0;
    trail.startWidth = 0.01f;
    trail.endWidth = 0.00f;
    trail.useWorldSpace = false;
    trail.startColor = trail.endColor = new Color(0.01f, 0.01f, 0.01f);
    trail.endColor = new Color(0, 0, 0);

    // trail.startWidth = 1.5f;
    // trail.endWidth = 0f;
    // trail.widthMultiplier = radius;
    // trail.time = 1.5f;
    // trail.minVertexDistance = 0.02f;
    // trail.startColor = trail.endColor = new Color(0.01f, 0.01f, 0.01f);
    // trail.material = mono.render.Material("Add");
  }

  public void Stop()
  {
    if (trail != null)
    {
      GameObject.Destroy(trail.gameObject);
    }
  }

  bool inside = false;
  public void Update()
  {
    // slower inside safeRadius
    float slow = 1;
    if (Vector3.Distance(mono.cursor, pos) > followDist)
    {
      if (pos.magnitude < mono.oriel.planetRadius)
      {
        if (!inside)
        {
          mono.sfx.Play("splash", pos, 0.33f);
          inside = true;
        }
        slow = 0.666f;
      }
      else
      {
        inside = false;
      }

      newPos = mono.cursor + (pos - mono.cursor).normalized * followDist;
      newPos.x = Mathf.Clamp(newPos.x, -mono.oriel.size.x / 2, mono.oriel.size.x / 2);
      newPos.y = Mathf.Clamp(newPos.y, -mono.oriel.size.y / 2, mono.oriel.size.y / 2);
      newPos.z = Mathf.Clamp(newPos.z, -mono.oriel.size.z / 2, mono.oriel.size.z / 2);
    }
    newPos = Vector3.Lerp(pos, newPos, Time.deltaTime * 60 * slow);
    vel = (newPos - pos).magnitude / Time.deltaTime;
    pos = newPos;

    dir = (mono.cursor - pos).normalized;


    // trail system
    Vector3 trailPos = pos;
    // -mono.oriel.transform.InverseTransformDirection(dir) * radius
    trail.positionCount += Mathf.Clamp(Mathf.RoundToInt(1 / Time.deltaTime / 3) - trail.positionCount, -1, 1);
    trail.SetPosition(0, trailPos * mono.oriel.scale);
    Vector3 lastPos = trailPos;
    for (int i = 0; i < trail.positionCount; i++)
    {
      if (trail.GetPosition(i) != Vector3.zero)
      {
        lastPos = trail.GetPosition(i);
      }
      else
      {
        trail.SetPosition(i, lastPos);
      }
    }

    for (int i = trail.positionCount - 1; i > 0; i--)
    {
      trail.SetPosition(i, trail.GetPosition(i - 1));
    }
  }
  Vector3 newPos = Vector3.zero;
}

[Serializable]
public class Gem : Detect
{
  Monolith mono;

  public float scale;
  public Color color;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    radius = 0.033f;

    Spawn();
  }

  public void Spawn()
  {
    pos = Vector3.zero;
    while (pos.magnitude < mono.oriel.safeRadius || Hit(mono.player))
    {
      pos = new Vector3(
        Random.Range(-mono.oriel.size.x / 2, mono.oriel.size.x / 2),
        Random.Range(-mono.oriel.size.y / 2, mono.oriel.size.y / 2),
        Random.Range(-mono.oriel.size.z / 2, mono.oriel.size.z / 2)
      );
    }

    scale = 0;
    color = PosColor();
  }

  float SmoothStep(float value, int pow)
  {
    return Mathf.Lerp(FakePow(value, pow), 1 - FakePow(1 - value, pow), value);
  }
  float FakePow(float value, int pow)
  {
    for (int i = 1; i < pow; i++)
    {
      value *= value;
    }
    return value;
  }

  Color PosColor()
  {
    float r = (pos.x / mono.oriel.size.x) + 0.5f;
    float g = (pos.y / mono.oriel.size.y) + 0.5f;
    float b = (pos.z / mono.oriel.size.z) + 0.5f;
    return new Color(SmoothStep(r, 6), SmoothStep(g, 6), SmoothStep(b, 6));
  }

  bool held = false;
  public void Update()
  {
    color = Color.Lerp(
      color,
      PosColor(),
      Time.deltaTime * pos.magnitude
    );

    if (!held)
    {
      if (Hit(mono.player))
      {
        mono.sfx.Play("pickup", pos);
        mono.render.PlayPS("GemPS", pos, Vector3.zero);

        held = true;
      }
    }
    if (held)
    {
      Vector3 targetPos = mono.player.pos + Quaternion.LookRotation(mono.player.dir) * new Vector3(0, -0.04f, 0.04f);
      pos = Vector3.Lerp(pos, targetPos, 0.5f);

      if (pos.magnitude < mono.oriel.planetRadius)
      {
        Vector3 dropPos = pos.normalized * mono.oriel.planetRadius;
        mono.sfx.Play("tree", dropPos, 0.25f);
        mono.render.PlayPS("TreeSpawnPS", dropPos, dropPos.normalized);
        mono.trees.Add(new Tree(dropPos, color));

        if (mono.enemies.Count == 0)
        {
          Enemy enemy = new Enemy();
          enemy.Start(mono, dropPos);
          mono.enemies.Add(enemy);
        }
        else
        {
          mono.enemies[0].Grow();
        }

        Spawn();

        mono.render.PlayPS("GemPS", dropPos, Vector3.zero);

        held = false;
      }
    }
    scale = Mathf.Clamp01(scale + Time.deltaTime * 3);
  }
}

[Serializable]
public class Tree : Detect
{
  public Color color;

  public Vector3 bend;

  public Tree(Vector3 pos, Color color)
  {
    this.pos = pos;
    this.color = color;
    this.radius = 0.02f;
    this.bend = pos.normalized;
  }
}

[Serializable]
public class Enemy : Detect
{
  Monolith mono;

  public Vector3 dir;
  public float scale;
  public List<Detect> segments;
  List<Vector3> pastPos;
  Vector3 oldPos;

  public void Start(Monolith mono, Vector3 spawnPos)
  {
    this.mono = mono;

    radius = 0.02f;

    pos = spawnPos;
    dir = spawnPos.normalized;
    pos += dir * mono.oriel.size.z * 3;
    dir *= -1;
    while (mono.oriel.OutOfBounds(pos) != Vector3.zero)
    {
      pos += dir * radius;
    }

    int cnt = 3;
    pastPos = new List<Vector3>();
    segments = new List<Detect>();
    for (int i = 0; i < cnt; i++)
    {
      pastPos.Add(pos);

      Detect segment = new Detect();
      segment.pos = pos;
      segment.radius = radius;
      segments.Add(segment);

      oldPos = pos;
    }

    spin = Random.rotation * Vector3.forward * Random.value;
    scale = 0;

    mono.render.PlayPS("EnemySpawnPS", pos, dir);
  }
  Vector3 spin = Vector3.forward;

  public void Grow()
  {
    // add pastPos and segment
    pastPos.Add(pos);

    Detect segment = new Detect();
    segment.pos = pos;
    segment.radius = radius;
    segments.Add(segment);
  }

  [HideInInspector]
  public bool bounced;
  float t;
  public void Update()
  {
    bounced = false;

    // move forward
    pos += dir * mono.player.speed * Time.deltaTime;
    Vector3 normal = mono.oriel.OutOfBounds(pos);
    if (normal != Vector3.zero && Vector3.Angle(normal, dir) > 90)
    {
      dir = Vector3.Reflect(dir, normal);
    }
    else
    {
      // reflect off of safeRadius
      float bounceRadius = mono.playing ? mono.oriel.safeRadius : mono.oriel.planetRadius;
      if (pos.magnitude < bounceRadius)
      {
        dir = Vector3.Reflect(dir, pos.normalized);
        pos += dir * mono.player.speed * Time.deltaTime;
        bounced = true;
      }
    }

    float timeBetween = 0.18f; // seconds between segments
    t += Time.deltaTime / timeBetween;
    if (t >= 1)
    {
      for (int i = pastPos.Count - 1; i > 0; i--)
      {
        pastPos[i] = pastPos[i - 1];
      }
      pastPos[0] = oldPos;
      oldPos = pos;

      t -= 1;
    }

    for (int i = pastPos.Count - 1; i > 0; i--)
    {
      segments[i].pos = Vector3.LerpUnclamped(pastPos[i], pastPos[i - 1], t);
    }
    segments[0].pos = Vector3.LerpUnclamped(pastPos[0], oldPos, t);


    scale = Mathf.Clamp01(scale + Time.deltaTime * 3);
  }
}

[Serializable]
public class Rig
{
  Monolith mono;

  [HideInInspector]
  public Camera headset, spectator;
  public PhysicalInput lHand, rHand;

  // LineRenderer lineCursor;
  public void Start(Monolith mono)
  {
    this.mono = mono;

    GameObject newObj = new GameObject("Headset");
    newObj.AddComponent<AudioListener>();
    newObj.tag = "MainCamera";
    headset = newObj.AddComponent<Camera>();
    headset.backgroundColor = Color.black;
    headset.nearClipPlane = 0.01f;
    headset.depth = -1;

    newObj = new GameObject("Spectator");
    newObj.transform.parent = headset.transform;
    newObj.tag = "Spectator";
    spectator = newObj.AddComponent<Camera>();
    spectator.stereoTargetEye = StereoTargetEyeMask.None;
    spectator.backgroundColor = Color.black;
    spectator.nearClipPlane = 0.01f;
    spectator.depth = 0;
    spectator.fieldOfView = 40;
    spectator.ResetAspect();

    // newObj = new GameObject("Cursor Line");
    // newObj.transform.parent = mono.oriel.transform;
    // lineCursor = newObj.AddComponent<LineRenderer>();
    // lineCursor.widthMultiplier = 0.003f;
    // lineCursor.startColor = lineCursor.endColor = new Color(0.01f, 0.01f, 0.01f, 1);
    // lineCursor.material = mono.render.Material("Add");

    // PlayerInput playerInput = mono.gameObject.GetComponent<PlayerInput>();
    // for (int i = 0; i < playerInput.currentActionMap.actions.Count; i++)
    // {
    //   Debug.Log(playerInput.currentActionMap.actions[i]);
    // }
  }

  bool lefty = false;
  XRHMD hmd;
  XRController lCon, rCon;
  public void Update()
  {
    if (hmd != null && hmd.wasUpdatedThisFrame)
    {
      headset.transform.position = hmd.centerEyePosition.ReadValue();
      headset.transform.rotation = hmd.centerEyeRotation.ReadValue();
    }
    else
    {
      hmd = InputSystem.GetDevice<XRHMD>();
    }

    if (lCon != null && lCon.wasUpdatedThisFrame)
    {
      lHand.pos = (Vector3)lCon.TryGetChildControl("pointerPosition").ReadValueAsObject();
      lHand.rot = (Quaternion)lCon.TryGetChildControl("pointerRotation").ReadValueAsObject();

      lHand.button.Set(lCon.TryGetChildControl("triggerpressed").IsPressed());
      lHand.altButton.Set(lCon.TryGetChildControl("primarybutton").IsPressed());
    }
    else
    {
      lCon = XRController.leftHand;
    }

    if (rCon != null && rCon.wasUpdatedThisFrame)
    {
      rHand.pos = (Vector3)rCon.TryGetChildControl("pointerPosition").ReadValueAsObject();
      rHand.rot = (Quaternion)rCon.TryGetChildControl("pointerRotation").ReadValueAsObject();

      rHand.button.Set(rCon.TryGetChildControl("triggerpressed").IsPressed());
      rHand.altButton.Set(rCon.TryGetChildControl("primarybutton").IsPressed());
    }
    else
    {
      rCon = XRController.rightHand;
    }

    Quaternion orielRot = Quaternion.Inverse(mono.oriel.transform.rotation);
    if (hmd != null)
    {
      if (lHand.button.held)
      {
        lefty = true;
      }
      if (rHand.button.held)
      {
        lefty = false;
      }

      PhysicalInput offHand = lHand;
      PhysicalInput mainHand = rHand;
      if (lefty)
      {
        offHand = rHand;
        mainHand = lHand;
      }

      dragPos = mainHand.pos;
      if (mainHand.button.down)
      {
        lastPos = dragPos;
      }
      if (mainHand.button.held)
      {
        mono.cursor += orielRot * headset.transform.rotation * ((dragPos - lastPos) * 12);

        // conteract hand drift
        mono.cursor = (orielRot * headset.transform.rotation) * Quaternion.Inverse(oldOrielRot) * mono.cursor;
        // mono.cursor = Parent(mono.cursor, Vector3.zero, (r) * Quaternion.Inverse(oldHeadsetRot));
      }
      mono.cursor = Vector3.ClampMagnitude(mono.cursor, mono.oriel.size.z * 2);

      // lineCursor.SetPosition(0, mono.oriel.transform.TransformPoint(mono.cursor));
      // lineCursor.SetPosition(1, mainHand.pos);

      lastPos = dragPos;
    }

    oldOrielRot = orielRot * headset.transform.rotation;
  }
  Quaternion oldOrielRot = Quaternion.identity;
  Vector3 lastPos, dragPos;

  public Vector3 Parent(Vector3 pos, Vector3 pivot, Quaternion rot)
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

  public Btn button, altButton;
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

  Material[] materials;
  Mesh[] meshes;

  [HideInInspector]
  public ParticleSystem[] particles;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    materials = Resources.LoadAll<Material>("Materials/");
    meshes = Resources.LoadAll<Mesh>("Meshes/");

    List<ParticleSystem> psList = new List<ParticleSystem>();
    for (int i = 0; i < mono.oriel.prefabs.Length; i++)
    {
      ParticleSystem ps = mono.oriel.prefabs[i].GetComponent<ParticleSystem>();
      if (ps != null)
      {
        psList.Add(ps);
      }
    }
    particles = psList.ToArray();

    ParticleSystem starPS = mono.oriel.GetPrefab("StarPS").GetComponent<ParticleSystem>();
    ParticleSystem.ShapeModule shape = starPS.shape;
    shape.shapeType = ParticleSystemShapeType.Box;
    shape.scale = mono.oriel.size * mono.oriel.scale;

    GameObject oriel = mono.oriel.GetPrefab("Oriel");
    LineRenderer[] lrs = oriel.GetComponentsInChildren<LineRenderer>();
    foreach (LineRenderer l in lrs)
    {
      l.widthMultiplier *= mono.oriel.scale;
    }
    oriel.transform.localScale = mono.oriel.size;
    oriel.transform.localPosition -= mono.oriel.size * 0.5f; // center it

    defaultProperties = new MaterialPropertyBlock();
    properties = new MaterialPropertyBlock();
  }

  Quaternion planetRot = Quaternion.identity;

  public void Update()
  {
    Shader.SetGlobalFloat("_Colored", mono.grayscale ? 0 : Mathf.Clamp01((Time.time - 3) / 3));

    DrawMesh("Oriel", "Oriel", true, Vector3.zero, Quaternion.identity, mono.oriel.size);
    // DrawMesh("Icosphere", "Add", false, mono.rig.lHand.pos, mono.rig.lHand.rot, 0.01f);
    // DrawMesh("Icosphere", "Add", false, mono.rig.rHand.pos, mono.rig.rHand.rot, 0.01f);
    DrawMesh("Cursor", "Always", true, mono.cursor, Vector3.forward, 0.02f);

    Quaternion planetRotDelta = Quaternion.Euler(0, Time.deltaTime * -6, 0);
    planetRot *= planetRotDelta;
    DrawMesh("Planet husk", "Default", true, Vector3.zero, planetRot, 0.01f);
    DrawMesh("Icosphere", "Water", true, Vector3.zero, planetRot, mono.oriel.planetRadius);

    if (mono.playing)
    {
      DrawMesh("Oriel Icosphere", "Sky", true, Vector3.zero, Vector3.forward, mono.oriel.safeRadius);

      DrawMesh("Bot for export no engons", "Default", true, mono.player.pos, mono.player.dir, 0.015f);
      if (mono.player.pos.magnitude > mono.oriel.safeRadius)
      {
        DrawMesh("headlights", "Add", true, mono.player.pos, mono.player.dir, 0.015f);
      }

      properties.SetColor("_Color", mono.gem.color);
      DrawMesh(
        "Gem2", "Gem", true,
        mono.gem.pos,
        Quaternion.Euler(Mathf.Sin(Time.time * 2) * 15, 0, Mathf.Sin(Time.time) * 15),
        PopIn(mono.gem.scale) * 0.0075f,
        properties
      );
    }

    foreach (Tree tree in mono.trees)
    {
      tree.pos = planetRotDelta * tree.pos;
      float t = Mathf.Max((tree.pos - mono.player.pos).magnitude / tree.radius / 3, 0.5f);
      Vector3 toDir = Vector3.Slerp((tree.pos - mono.player.pos).normalized, tree.pos.normalized, t);
      tree.bend = Vector3.Slerp(tree.bend, toDir, Time.deltaTime * Mathf.Max(mono.player.vel, 0.1f) * 20);
      properties.SetColor("_Color", tree.color);
      DrawMesh("Tree ",
        "Gem",
        true,
        tree.pos,
        tree.bend,
        0.004f, properties
      );
    }

    foreach (Enemy enemy in mono.enemies)
    {
      DrawMesh("New Enemy head E", "Default", true, enemy.pos, -enemy.dir, 0.01f * PopIn(enemy.scale));
      Vector3 lastPos = enemy.pos;
      for (int j = 0; j < enemy.segments.Count; j++)
      {
        DrawMesh(
          j < enemy.segments.Count - 1 ? "New Enemy body E" : "New Enemy tail E",
          "Default",
          true,
          enemy.segments[j].pos,
          enemy.segments[j].pos - lastPos,
          0.01f * PopIn(enemy.scale)
        );
        lastPos = enemy.segments[j].pos;
      }
    }

    // if (true)
    // {
    //   // DrawMesh(meshSphere, matDebug, mono.cursor, Quaternion.identity, mono.player.followDist / 2);
    //   DrawMesh(meshSphere, matDebug, mono.player.pos, Quaternion.identity, mono.player.radius / 2);
    //   DrawMesh(meshSphere, matDebug, mono.gem.pos, Quaternion.identity, mono.gem.radius / 2);
    //   for (int i = 0; i < mono.enemies.Count; i++)
    //   {
    //     DrawMesh(meshSphere, matDebug, mono.enemies[i].pos, Quaternion.identity, mono.enemies[i].radius / 2);
    //   }
    // }
  }

  Matrix4x4 m4 = new Matrix4x4();
  MaterialPropertyBlock properties, defaultProperties;

  void DrawMesh(
    string mesh,
    string material,
    bool inOriel,
    Vector3 pos,
    Quaternion rot,
    Vector3 scale,
    MaterialPropertyBlock props = null
  )
  {
    m4.SetTRS(
      inOriel ? mono.oriel.transform.TransformPoint(pos) : pos,
      inOriel ? mono.oriel.transform.rotation * rot.normalized : rot.normalized,
      inOriel ? mono.oriel.scale * scale : scale
    );
    Graphics.DrawMesh(
      Mesh(mesh), m4,
      Material(material),
      0, null, 0,
      props == null ? defaultProperties : props
    );
  }

  void DrawMesh(
    string mesh,
    string material,
    bool inOriel,
    Vector3 pos,
    Vector3 dir,
    float scale = 1,
    MaterialPropertyBlock props = null
  )
  {
    DrawMesh(mesh, material, inOriel, pos, Quaternion.LookRotation(dir), Vector3.one * scale, props);
  }

  void DrawMesh(
    string mesh,
    string material,
    bool inOriel,
    Vector3 pos,
    Quaternion rot,
    float scale = 1,
    MaterialPropertyBlock props = null
  )
  {
    DrawMesh(mesh, material, inOriel, pos, rot, Vector3.one * scale, props);
  }

  public Material Material(string name)
  {
    for (int i = 0; i < materials.Length; i++)
    {
      if (name == materials[i].name)
      {
        return materials[i];
      }
    }
    Debug.LogWarning("Material not found: " + name);
    return null;
  }

  public Mesh Mesh(string name)
  {
    for (int i = 0; i < meshes.Length; i++)
    {
      if (meshes[i].name == name)
      {
        return meshes[i];
      }
    }
    Debug.LogWarning("Mesh not found: " + name);
    return null;
  }

  public void PlayPS(string name, Vector3 pos, Vector3 dir)
  {
    if (dir == Vector3.zero) { dir = Vector3.forward; }
    for (int i = 0; i < particles.Length; i++)
    {
      ParticleSystem ps = particles[i];
      if (ps.gameObject.name == name)
      {
        ps.transform.localPosition = pos;
        ps.transform.localRotation = Quaternion.LookRotation(dir);
        ps.Play();
        return;
      }
    }
  }

  float PopIn(float scale)
  {
    // init
    if (scale > 0 && scale < 0.333f)
    {
      return 0.25f;
    }
    if (scale >= 0.333f && scale < 0.666f)
    {
      return 0.75f;
    }
    if (scale >= 0.666f && scale < 1)
    {
      return 1.25f;
    }

    // grow
    // extend
    // settle

    return scale;
  }
}

[Serializable]
public class SFX
{
  Monolith mono;

  List<AudioSource> srcs = new List<AudioSource>();
  AudioClip[] clips;

  AudioSource jet;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    for (int i = 0; i < 6; i++)
    {
      GameObject go = new GameObject("SFX " + i);
      AudioSource src = go.AddComponent<AudioSource>();
      src.spatialBlend = 1;
      srcs.Add(src);
    }

    clips = Resources.LoadAll<AudioClip>("SFX/");


    jet = new GameObject("SFX jet").AddComponent<AudioSource>();
    jet.spatialBlend = 1;
    jet.loop = true;
    jet.clip = GetClip("jet");
    jet.volume = 0;
    jet.dopplerLevel = 1;
    jet.Play();
  }

  public void Update()
  {
    jet.transform.position = mono.player.pos;
    jet.volume = Mathf.Clamp01(mono.player.vel / 6);
    jet.pitch = 1 + mono.player.vel;
  }

  public void Play(string name, Vector3 pos, float volume = 1)
  {
    for (int i = 0; i < srcs.Count; i++)
    {
      AudioSource src = srcs[i];
      if (!src.isPlaying)
      {
        AudioClip clip = GetClip(name);
        if (clip != null)
        {
          src.transform.position = pos;

          src.clip = clip;
          src.volume = volume;
          src.Play();
          return;
        }
      }
    }
  }

  public AudioClip GetClip(string name)
  {
    for (int j = 0; j < clips.Length; j++)
    {
      if (name == clips[j].name)
      {
        return clips[j];
      }
    }
    Debug.LogWarning("AudioClip not found: " + name);
    return null;
  }
}

[Serializable]
public class Music
{
  Monolith mono;

  public AudioMixer mixer;
  public AudioMixerGroup mixerGroup;
  AudioSource srcMenu, srcGame;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    GameObject newSrc = new GameObject("Menu Music");
    srcMenu = newSrc.AddComponent<AudioSource>();
    srcMenu.outputAudioMixerGroup = mixerGroup;
    srcMenu.clip = Resources.Load<AudioClip>("menu");
    srcMenu.loop = true;
    srcMenu.volume = 0;
    srcMenu.Play();

    newSrc = new GameObject("Game Music");
    srcGame = newSrc.AddComponent<AudioSource>();
    srcGame.outputAudioMixerGroup = mixerGroup;
    srcGame.clip = Resources.Load<AudioClip>("game");
    srcGame.loop = true;
    srcGame.volume = 0;
    srcGame.Play();

  }

  public void Update()
  {
    float menuVol = mono.playing ? 0 : 1;
    srcMenu.volume = Mathf.Lerp(srcMenu.volume, menuVol, Time.deltaTime / 3);

    float gameVol = mono.playing ? 1 : 0;
    srcGame.volume = Mathf.Lerp(srcGame.volume, gameVol, Time.deltaTime / 3);


    if (!mono.grayscale && !transitioned && Time.time > 3)
    {
      mixer.FindSnapshot("Color").TransitionTo(3);
      transitioned = true;
    }
  }
  bool transitioned = false;
}

[Serializable]
public class ScreenCap
{
  Monolith mono;
#if (UNITY_EDITOR)
  RecorderController m_RecorderController;
#endif

  public void Start(Monolith mono)
  {
    this.mono = mono;

#if (UNITY_EDITOR)
    RecorderControllerSettingsPreset preset = Resources.Load<RecorderControllerSettingsPreset>("RCSettings");
    RecorderControllerSettings settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
    preset.ApplyTo(settings);
    m_RecorderController = new RecorderController(settings);
#endif
  }

  public void Update()
  {
#if (UNITY_EDITOR)
    if (mono.rig.rHand.altButton.down)
    {
      m_RecorderController.PrepareRecording();
      m_RecorderController.StartRecording();
    }
#endif
  }
}
