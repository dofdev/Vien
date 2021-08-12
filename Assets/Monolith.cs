using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif
using TMPro;

using Random = UnityEngine.Random;

public class Monolith : MonoBehaviour
{
  public Rig rig;
  public Player player;
  public Gem gem;
  public List<Tree> trees = new List<Tree>();
  public List<Enemy> enemies = new List<Enemy>();
  public Render render;
  public SFX sfx;
  public Music music;
  public ScreenCap screenCap;
  [HideInInspector]
  public TextMeshPro textMesh;

  // public bool paid = false;

  public Vector3 cursor;
  [HideInInspector]
  public Vector3 oriel;
  [HideInInspector]
  public float safeRadius;
  [HideInInspector]
  public float planetRadius;

  void Awake()
  {
    oriel = new Vector3(0.8f, 0.7f, 0.8f);
    safeRadius = 0.145f;
    planetRadius = 0.1f;

    GameObject go = new GameObject();
    go.transform.position = Vector3.back * oriel.z / 2;
    textMesh = go.AddComponent<TextMeshPro>();
    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
    textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
    textMesh.fontSize = 1;

    render.Start(this);
    rig.Start(this);
    sfx.Start(this);
    music.Start(this);
    screenCap.Start(this);
  }

  void Start()
  {
    trees.Clear();
    for (int i = 0; i < enemies.Count; i++)
    {
      enemies[i].Stop();
    }
    enemies.Clear();
    player.Stop();

    player.Start(this);
    gem.Start(this);

    textMesh.text = "START";
  }

  [HideInInspector]
  public bool playing = false;
  void Update()
  {
    rig.Update();

    if (!playing)
    {
      Mouse mouse = Mouse.current;
      if (rig.rHand.button.down || rig.lHand.button.down || (mouse != null && Mouse.current.leftButton.IsPressed()))
      {
        sfx.Play("button");
        Start();
        textMesh.text = "";
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
        if (enemies[i].Hit(player))
        {
          textMesh.text = trees.Count + " <br>RESET?";
          sfx.Play("gameover");
          sfx.Play("explosion");
          playing = false;
          // alternative ending is where all the enemies target what spawned them, and phase out
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
  [HideInInspector]
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
  [HideInInspector]
  public float followDist;
  [HideInInspector]
  public float speed;

  TrailRenderer trail;
  public void Start(Monolith mono)
  {
    this.mono = mono;

    pos = Vector3.zero;
    dir = Vector3.back;
    radius = 0.02f;
    followDist = 0.09f;
    speed = 0.2f;

    GameObject newObj = new GameObject();
    newObj.transform.position = pos;
    trail = newObj.AddComponent<TrailRenderer>();
    trail.startWidth = 1.5f;
    trail.endWidth = 1f;
    trail.widthMultiplier = radius;
    trail.time = 3f;
    trail.minVertexDistance = 0.02f;
    trail.startColor = new Color(0.05f, 0.05f, 0.05f, 1);
    trail.endColor = Color.black;
    trail.material = mono.render.Mat("Add");
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
      if (pos.magnitude < mono.planetRadius)
      {
        if (!inside)
        {
          mono.sfx.Play("splash", 0.33f);
          inside = true;
        }
        slow = 0.666f;
      }
      else
      {
        inside = false;
      }
      // Vector3 newPos = pos + (mono.cursor - pos).normalized * speed * slow * Time.deltaTime;
      // one to one then apply slow
      newPos = mono.cursor + (pos - mono.cursor).normalized * followDist;
      newPos.x = Mathf.Clamp(newPos.x, -mono.oriel.x / 2, mono.oriel.x / 2);
      newPos.y = Mathf.Clamp(newPos.y, -mono.oriel.y / 2, mono.oriel.y / 2);
      newPos.z = Mathf.Clamp(newPos.z, -mono.oriel.z / 2, mono.oriel.z / 2);
    }
    pos = Vector3.Lerp(pos, newPos, Time.deltaTime * 6 * slow);

    trail.transform.position = pos;

    dir = (mono.cursor - pos).normalized;
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

    radius = 0.02f;

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
    scale = 0;


    // set colors based on position (color cube: xyz -> rgb)
    // normalize based on oriel size


    // List<Color> colors = new List<Color>();
    // for (int i = 0; i < mono.render.meshGem.vertexCount; i++)
    // {
    //   colors.Add(color);
    // }
    // mono.render.meshGem.SetColors(colors);
    // mono.render.meshGem.MarkModified();
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

  bool held = false;
  public void Update()
  {
    float r = (pos.x / mono.oriel.x) + 0.5f;
    float g = (pos.y / mono.oriel.y) + 0.5f;
    float b = (pos.z / mono.oriel.z) + 0.5f;
    // Mathf.SmoothStep
    color = Color.Lerp(
      color,
      new Color(SmoothStep(r, 6), SmoothStep(g, 6), SmoothStep(b, 6)),
      Time.deltaTime * pos.magnitude
    );

    if (!held)
    {
      if (Hit(mono.player))
      {
        mono.sfx.Play("pickup");
        held = true;
      }
    }
    if (held)
    {
      Vector3 targetPos = mono.player.pos + Quaternion.LookRotation(mono.player.dir) * new Vector3(0, -0.04f, 0.04f);
      pos = Vector3.Lerp(pos, targetPos, 0.5f);

      if (pos.magnitude < mono.planetRadius)
      {
        mono.sfx.Play("tree");
        mono.trees.Add(new Tree(pos, color));

        Enemy enemy = new Enemy();
        enemy.Start(mono, pos);
        mono.enemies.Add(enemy);
        Spawn();

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

  public Tree(Vector3 pos, Color color)
  {
    this.pos = pos;
    this.color = color;
    this.radius = 0.02f;
  }
}

[Serializable]
public class Enemy : Detect
{
  Monolith mono;

  public Vector3 dir;
  public Quaternion rot;
  public float scale;
  public Vector3[] pastPos;

  // public Enemy() {}

  // public Enemy(Monolith mono, Enemy other)
  // {
  //   this.mono = mono;
  //   pos = other.pos;
  //   radius = other.radius;
  //   dir = other.dir;
  //   rot = other.rot;
  //   scale = other.scale;
  // }

  // TrailRenderer trail;
  public void Start(Monolith mono, Vector3 spawnPos)
  {
    this.mono = mono;

    radius = 0.02f;

    // spawn out, then clamp in
    // pos = Random.rotation * Vector3.forward * mono.oriel.x * 2;
    // pos.x = Mathf.Clamp(pos.x, (-mono.oriel.x / 2) + radius * 2, (mono.oriel.x / 2) - radius * 2);
    // pos.y = Mathf.Clamp(pos.y, (-mono.oriel.y / 2) + radius * 2, (mono.oriel.y / 2) - radius * 2);
    // pos.z = Mathf.Clamp(pos.z, (-mono.oriel.z / 2) + radius * 2, (mono.oriel.z / 2) - radius * 2);
    // dir = Random.rotation * Vector3.forward;
    pos = spawnPos;
    dir = spawnPos.normalized;
    while (Hit(mono.player) || pos.magnitude < mono.safeRadius)
    {
      pos += dir * radius;
    }

    pastPos = new Vector3[5];
    for (int i = 0; i < pastPos.Length; i++)
    {
      pastPos[i] = pos;
    }

    // GameObject newObj = new GameObject();
    // newObj.transform.position = pos;
    // trail = newObj.AddComponent<TrailRenderer>();
    // trail.startWidth = 1.5f;
    // trail.endWidth = 1f;
    // trail.widthMultiplier = radius;
    // trail.time = 3f;
    // trail.minVertexDistance = 0.02f;
    // trail.startColor = new Color(0.05f, 0.05f, 0.05f, 1);
    // trail.endColor = Color.black;
    // trail.material = mono.render.matPS;

    rot = Random.rotation;
    spin = Random.rotation * Vector3.forward * Random.value;
    scale = 0;
  }
  Vector3 spin = Vector3.forward;

  public void Stop()
  {
    // GameObject.Destroy(trail.gameObject);
  }

  [HideInInspector]
  public bool bounced;
  float delay;
  public void Update()
  {
    bounced = false;

    // move forward
    pos += dir * mono.player.speed * Time.deltaTime;
    Vector3 normal = mono.OutOfBounds(pos);
    if (normal != Vector3.zero && Vector3.Angle(normal, dir) > 90)
    {
      dir = Vector3.Reflect(dir, normal);
    }
    else
    {
      // reflect off of safeRadius
      float bounceRadius = mono.playing ? mono.safeRadius : mono.planetRadius;
      if (pos.magnitude < bounceRadius)
      {
        dir = Vector3.Reflect(dir, pos.normalized);
        pos += dir * mono.player.speed * Time.deltaTime;
        bounced = true;
      }
    }

    if (Time.time >= delay)
    {
      for (int i = pastPos.Length - 1; i >= 0; i--)
      {
        pastPos[i] = pastPos[Mathf.Max(i - 1, 0)];
      }
      pastPos[0] = pos;
      delay = Time.time + 0.333f;
    }

    // trail.transform.position = pos;
    rot *= Quaternion.Euler(spin * Time.deltaTime * 120);
    scale = Mathf.Clamp01(scale + Time.deltaTime * 3);
  }
}

[Serializable]
public class Rig
{
  Monolith mono;

  public Camera cam;
  public PhysicalInput lHand, rHand;

  [HideInInspector]
  public Vector3 offset;
  [HideInInspector]
  public float scale;

  LineRenderer lineCursor;
  public void Start(Monolith mono)
  {
    this.mono = mono;

    offset = new Vector3(0, 0.1f, -1.8f);
    scale = 2;

    GameObject newObj = new GameObject();
    lineCursor = newObj.AddComponent<LineRenderer>();
    lineCursor.widthMultiplier = 0.006f;
    lineCursor.startColor = new Color(0.05f, 0.05f, 0.05f, 1);
    lineCursor.endColor = new Color(0.05f, 0.05f, 0.05f, 1);
    lineCursor.material = mono.render.Mat("Add");
  }

  bool lefty = false;
  public void Update()
  {
    Vector3 rigPos = Vector3.zero;
    Quaternion rigRot = Quaternion.identity;

    XRHMD hmd = InputSystem.GetDevice<XRHMD>();
    if (hmd != null)
    {
      Vector3 headPos = hmd.centerEyePosition.ReadValue() * 2;
      Quaternion headRot = hmd.centerEyeRotation.ReadValue();
      // jitter *= -1;
      // headRot *= Quaternion.Euler(0, jitter, 0);
      rigPos = -headPos + (headRot * offset);

      cam.transform.position = Parent(headPos, rigPos, rigRot);
      cam.transform.rotation = rigRot * headRot;
      cam.transform.localScale = Vector3.one * scale;
    }

    XRController lCon = XRController.leftHand;
    if (lCon != null)
    {
      lHand.localPos = lCon.devicePosition.ReadValue();
      lHand.pos = Parent(lHand.localPos * scale, rigPos, rigRot);
      lHand.rot = rigRot * lCon.deviceRotation.ReadValue();

      lHand.button.Set(lCon.TryGetChildControl("triggerpressed").IsPressed());
      lHand.altButton.Set(lCon.TryGetChildControl("primarybutton").IsPressed());

      // foreach (InputControl ic in lCon.children)
      // {
      //   Debug.Log(ic.name);
      // }
      // Debug.LogError("WOWWEEE");
    }

    XRController rCon = XRController.rightHand;
    if (rCon != null)
    {
      rHand.localPos = rCon.devicePosition.ReadValue();
      rHand.pos = Parent(rHand.localPos * scale, rigPos, rigRot);
      rHand.rot = rigRot * rCon.deviceRotation.ReadValue();

      rHand.button.Set(rCon.TryGetChildControl("triggerpressed").IsPressed());
      rHand.altButton.Set(rCon.TryGetChildControl("primarybutton").IsPressed());
    }

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

      if (mainHand.button.down)
      {
        offsetCursor = mono.cursor - Parent(localCursor * scale, rigPos, rigRot);
        lastPos = mainHand.localPos;
      }
      if (mainHand.button.held)
      {
        localCursor += (mainHand.localPos - lastPos) * 3;
        mono.cursor = Parent(localCursor * scale, rigPos, rigRot) + offsetCursor;
      }

      lineCursor.SetPosition(0, mono.cursor);
      lineCursor.SetPosition(1, mainHand.pos);

      lastPos = mainHand.localPos;
    }
  }
  Vector3 lastPos, localCursor, offsetCursor;

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
  public Vector3 localPos, pos;
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
  public Mesh[] meshMeteors;
  public LineRenderer orielLine;

  Quaternion planetRot = Quaternion.identity;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    materials = Resources.LoadAll<Material>("Materials/");
    meshes = Resources.LoadAll<Mesh>("Meshes/");
    Debug.Log("Meshes Loaded:");
    for (int i = 0; i < meshes.Length; i++)
    {
      Debug.Log(meshes[i].name);
    }

    ParticleSystem ps = mono.gameObject.AddComponent<ParticleSystem>();
    ParticleSystem.ShapeModule shape = ps.shape;
    shape.shapeType = ParticleSystemShapeType.Box;
    shape.scale = mono.oriel;
    ParticleSystem.MainModule main = ps.main;
    main.startSpeed = 0;
    main.startSize = 0.001f;
    // main.startColor.mode = ParticleSystemGradientMode.Gradient;
    // ParticleSystem.MinMaxGradient gradient = main.startColor.gradient;
    // gradient.mode = ParticleSystemGradientMode.Gradient;
    // GradientColorKey[] keys = new GradientColorKey[2];
    // keys[0] = new GradientColorKey(Color.red, 0);
    // keys[1] = new GradientColorKey(Color.blue, 1);
    // GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
    // alphaKeys[0] = new GradientAlphaKey(1, 0);
    // alphaKeys[1] = new GradientAlphaKey(1, 1);
    // gradient.SetKeys(keys, alphaKeys);
    // main.startColor = gradient;
    ParticleSystemRenderer psr = mono.gameObject.GetComponent<ParticleSystemRenderer>();
    psr.material = Mat("Add");

    orielLine.transform.localScale = mono.oriel;
    orielLine.transform.position -= mono.oriel * 0.5f;

    properties = new MaterialPropertyBlock();
  }

  public void Update()
  {
    DrawMesh(Mesh("Controller Low poly.001"), Mat("Default"), mono.rig.lHand.pos, mono.rig.lHand.rot, 0.03f);
    DrawMesh(Mesh("Controller Low poly.001"), Mat("Default"), mono.rig.rHand.pos, mono.rig.rHand.rot, 0.03f);

    // m4.SetTRS(Vector3.zero, Quaternion.identity, mono.oriel);
    // Graphics.DrawMesh(meshOriel, m4, matOriel, 0);
    // DrawMesh(meshStart, matUI, Vector3.back * 0.5f, Quaternion.Euler(-90, 0, 0), 0.05f);

    Quaternion planetTurn = Quaternion.Euler(0, Time.deltaTime * -6, 0);
    planetRot *= planetTurn;
    DrawMesh(Mesh("Planet husk"), Mat("Default"), Vector3.zero, planetRot, 0.01f);
    DrawMesh(Mesh("Icosphere"), Mat("Water"), Vector3.zero, planetRot, mono.planetRadius);

    DrawMesh(Mesh("Cursor"), Mat("Add"), mono.cursor, Quaternion.identity, 0.02f);

    if (mono.playing)
    {
      DrawMesh(Mesh("Oriel Icosphere"), Mat("Sky"), Vector3.zero, Quaternion.identity, mono.safeRadius);
      
      DrawMesh(Mesh("Bot for export no engons"), Mat("Default"), mono.player.pos, Quaternion.LookRotation(mono.player.dir), 0.015f);
      if (mono.player.pos.magnitude > mono.safeRadius)
      {
        DrawMesh(Mesh("headlights"), Mat("Add"), mono.player.pos, Quaternion.LookRotation(mono.player.dir), 0.015f);
      }

      m4.SetTRS(
          mono.gem.pos,
          Quaternion.Euler(Mathf.Sin(Time.time * 2) * 15, 0, Mathf.Sin(Time.time) * 15),
          Vector3.one * PopIn(mono.gem.scale) * 0.0075f
        );
      // MaterialPropertyBlock properties = new MaterialPropertyBlock();
      properties.SetColor("_Color", mono.gem.color);
      Graphics.DrawMesh(Mesh("Gem2"), m4, Mat("Gem"), 0, null, 0, properties);
    }


    for (int i = 0; i < mono.trees.Count; i++)
    {
      Tree tree = mono.trees[i];
      tree.pos = planetTurn * tree.pos;
      // mono.render.matGem.SetColor("_Color", tree.color);
      // DrawMesh(meshTree, matGem,
      //   tree.pos, Quaternion.LookRotation(tree.pos), 0.005f);

      m4.SetTRS(
        tree.pos,
        Quaternion.LookRotation(tree.pos),
        Vector3.one * 0.004f
      );
      // properties = new MaterialPropertyBlock();
      properties.SetColor("_Color", tree.color);
      Graphics.DrawMesh(Mesh("Tree "), m4, Mat("Gem"), 0, null, 0, properties);
    }

    for (int i = 0; i < mono.enemies.Count; i++)
    {
      int meshIndex = i;
      while (meshIndex > meshMeteors.Length - 1)
      {
        meshIndex -= meshMeteors.Length;
      }
      DrawMesh(meshMeteors[meshIndex], Mat("Default"),
        mono.enemies[i].pos, mono.enemies[i].rot, 0.01f * PopIn(mono.enemies[i].scale));
      for (int j = 0; j < mono.enemies[i].pastPos.Length; j++)
      {
        DrawMesh(meshMeteors[meshIndex], Mat("Default"),
        mono.enemies[i].pastPos[j], mono.enemies[i].rot, 0.01f * PopIn(mono.enemies[i].scale));
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
  MaterialPropertyBlock properties;

  void DrawMesh(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, float scale)
  {
    m4.SetTRS(pos, rot.normalized, Vector3.one * scale);
    Graphics.DrawMesh(mesh, m4, mat, 0);
  }

  public Material Mat(string name)
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

  List<GameObject> srcs = new List<GameObject>();
  AudioClip[] clips;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    for (int i = 0; i < 6; i++)
    {
      GameObject newSrc = new GameObject();
      newSrc.AddComponent<AudioSource>();
      srcs.Add(newSrc);
    }

    clips = Resources.LoadAll<AudioClip>("SFX/");
  }

  public void Update()
  {

  }

  public void Play(string name, float volume = 1)
  {
    for (int i = 0; i < srcs.Count; i++)
    {
      AudioSource src = srcs[i].GetComponent<AudioSource>();
      if (!src.isPlaying)
      {
        for (int j = 0; j < clips.Length; j++)
        {
          if (name == clips[j].name)
          {
            src.clip = clips[j];
            src.volume = volume;
            src.Play();
            return;
          }
        }
      }
    }
  }
}

[Serializable]
public class Music
{
  Monolith mono;

  AudioSource srcMenu, srcGame;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    GameObject newSrc = new GameObject();
    srcMenu = newSrc.AddComponent<AudioSource>();
    srcMenu.clip = Resources.Load<AudioClip>("menu");
    srcMenu.loop = true;
    srcMenu.volume = 0;
    srcMenu.Play();

    newSrc = new GameObject();
    srcGame = newSrc.AddComponent<AudioSource>();
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
  }
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
