using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using TMPro;

using Random = UnityEngine.Random;

public class Monolith : MonoBehaviour
{
  public Rig rig;
  public Player player;
  public Gem gem;
  public List<Vector3> trees = new List<Vector3>();
  public List<Enemy> enemies = new List<Enemy>();
  public Render render;
  public SFX sfx;
  public Music music;
  public ScreenCap screenCap;
  [HideInInspector]
  public TextMeshPro textMesh;

  public Vector3 cursor;
  [HideInInspector]
  public Vector3 oriel;
  [HideInInspector]
  public float safeRadius = 0.2f;

  void Awake()
  {
    oriel = new Vector3(0.8f, 0.7f, 0.8f);
    safeRadius = 0.1f;

    GameObject go = new GameObject();
    go.transform.position = Vector3.back * oriel.z / 2;
    textMesh = go.AddComponent<TextMeshPro>();
    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
    textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
    textMesh.fontSize = 1;

    rig.Start(this);
    render.Start(this);
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
      for (int i = 0; i < enemies.Count; i++)
      {
        enemies[i].Update();
        if (enemies[i].Hit(player))
        {
          textMesh.text = trees.Count + " <br>RESET?";
          sfx.Play("gameover");
          sfx.Play("explosion");
          playing = false;
          // alternative ending is where all the enemies target what spawned them, and phase out
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
    speed = 0.3f;

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
    trail.material = mono.render.matPS;
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
    if (Vector3.Distance(mono.cursor, pos) > followDist)
    {
      // slower inside safeRadius
      float slow = 1;
      if (pos.magnitude < mono.safeRadius)
      {
        if (!inside)
        {
          mono.sfx.Play("splash", 0.33f);
          inside = true;
        }
        slow = 0.5f;
      }
      else
      {
        inside = false;
      }
      Vector3 newPos = pos + (mono.cursor - pos).normalized * speed * slow * Time.deltaTime;
      if (mono.OutOfBounds(newPos) == Vector3.zero)
      {
        pos = newPos;
      }
    }

    trail.transform.position = pos;

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
  }

  bool held = false;
  public void Update()
  {
    if (!held && Hit(mono.player))
    {
      mono.sfx.Play("pickup");
      held = true;
    }
    if (held)
    {
      pos = mono.player.pos + Vector3.down * mono.player.radius * 2;

      if (pos.magnitude < mono.safeRadius)
      {
        mono.sfx.Play("tree");
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
  public Quaternion rot;

  TrailRenderer trail;
  public void Start(Monolith mono)
  {
    this.mono = mono;

    radius = 0.02f;

    // spawn out, then clamp in
    pos = Random.rotation * Vector3.forward * mono.oriel.x * 2;
    pos.x = Mathf.Clamp(pos.x, (-mono.oriel.x / 2) + radius * 2, (mono.oriel.x / 2) - radius * 2);
    pos.y = Mathf.Clamp(pos.y, (-mono.oriel.y / 2) + radius * 2, (mono.oriel.y / 2) - radius * 2);
    pos.z = Mathf.Clamp(pos.z, (-mono.oriel.z / 2) + radius * 2, (mono.oriel.z / 2) - radius * 2);
    dir = Random.rotation * Vector3.forward;

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
    trail.material = mono.render.matPS;

    rot = Random.rotation;
    spin = Random.rotation * Vector3.forward;
  }
  Vector3 spin = Vector3.forward;

  public void Stop()
  {
    GameObject.Destroy(trail.gameObject);
  }

  public void Update()
  {
    // move forward
    pos += dir * mono.player.speed * 0.5f * Time.deltaTime;
    Vector3 normal = mono.OutOfBounds(pos);
    if (normal != Vector3.zero && Vector3.Angle(normal, dir) > 90)
    {
      dir = Vector3.Reflect(dir, normal);
    }
    else
    {
      // reflect off of safeRadius
      if (pos.magnitude < mono.safeRadius)
      {
        dir = Vector3.Reflect(dir, pos.normalized);
        pos += dir * mono.player.speed * 0.5f * Time.deltaTime;
      }
    }

    trail.transform.position = pos;
    rot *= Quaternion.Euler(spin * Time.deltaTime * 12);
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

  LineRenderer lineCursor, lineStretch;
  public void Start(Monolith mono)
  {
    this.mono = mono;

    offset = new Vector3(0, 0.1f, -1.8f);
    scale = 2;

    GameObject newObj = new GameObject();
    lineCursor = newObj.AddComponent<LineRenderer>();
    lineCursor.widthMultiplier = 0.006f;
    lineCursor.material = mono.render.matDebug;

    newObj = new GameObject();
    lineStretch = newObj.AddComponent<LineRenderer>();
    lineStretch.material = mono.render.matDebug;
  }

  Vector3 cursorDir = Vector3.forward;
  float cursorDist = 0f;
  float stretchMid = 0.67f;
  float stretchScale = 3;
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
      rHand.pos = Pivot(rCon.devicePosition.ReadValue() * scale, rigPos, rigRot);
      rHand.rot = rigRot * rCon.deviceRotation.ReadValue();

      rHand.button.Set(rCon.TryGetChildControl("triggerpressed").IsPressed());
      rHand.altButton.Set(rCon.TryGetChildControl("primarybutton").IsPressed());
    }

    if (hmd != null)
    {
      bool recalibrate = false;
      if (lHand.button.held)
      {
        lefty = true;
        recalibrate = true;
      }
      if (rHand.button.held)
      {
        lefty = false;
        recalibrate = true;
      }

      PhysicalInput offHand = lHand;
      PhysicalInput mainHand = rHand;
      if (lefty)
      {
        offHand = rHand;
        mainHand = lHand;
      }

      float handDist = Vector3.Distance(mainHand.pos, offHand.pos);
      if (recalibrate)
      {
        cursorDir = Quaternion.Inverse(mainHand.rot) * -mainHand.pos.normalized;
        cursorDist = mainHand.pos.magnitude;
        stretchMid = handDist;
      }

      float stretch = handDist - stretchMid;
      lineStretch.SetPosition(0, offHand.pos);
      lineStretch.SetPosition(1, mainHand.pos);
      lineStretch.widthMultiplier = 0.03f * ((stretchMid * 3) - Mathf.Clamp(handDist, 0, (stretchMid * 3) - 0.1f));

      mono.cursor = mainHand.pos + mainHand.rot * cursorDir * (cursorDist + (stretch * stretchScale));
      lineCursor.SetPosition(0, mono.cursor);
      lineCursor.SetPosition(1, mainHand.pos);
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

  public Material matDefault, matOriel, matDebug, matPS, matWater;
  public Mesh meshCube, meshSphere, meshOriel, meshWorld, meshGem, meshTree, meshPlayer, meshEnemy, meshCursor;

  Quaternion planetRot = Quaternion.identity;

  public void Start(Monolith mono)
  {
    this.mono = mono;

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
    psr.material = matPS;
  }

  public void Update()
  {
    DrawMesh(meshCube, matDefault, mono.rig.lHand.pos, mono.rig.lHand.rot, 0.03f);
    DrawMesh(meshCube, matDefault, mono.rig.rHand.pos, mono.rig.rHand.rot, 0.03f);

    m4.SetTRS(Vector3.zero, Quaternion.identity, mono.oriel);
    Graphics.DrawMesh(meshOriel, m4, matOriel, 0);
    // DrawMesh(meshStart, matUI, Vector3.back * 0.5f, Quaternion.Euler(-90, 0, 0), 0.05f);

    Quaternion planetTurn = Quaternion.Euler(0, Time.deltaTime * -6, 0);
    planetRot *= planetTurn;
    DrawMesh(meshWorld, matDefault, Vector3.zero, planetRot, 0.01f);
    DrawMesh(meshSphere, matWater, Vector3.zero, planetRot, 5f);

    DrawMesh(meshCursor, matDefault, mono.cursor, Quaternion.identity, 0.02f);

    DrawMesh(meshPlayer, matDefault, mono.player.pos, Quaternion.LookRotation(mono.player.dir), 0.02f);

    DrawMesh(meshGem, matDefault, mono.gem.pos, Quaternion.identity, 0.01f);

    for (int i = 0; i < mono.trees.Count; i++)
    {
      mono.trees[i] = planetTurn * mono.trees[i];
      DrawMesh(meshTree, matDefault,
        mono.trees[i], Quaternion.LookRotation(mono.trees[i]), 0.005f);
    }

    for (int i = 0; i < mono.enemies.Count; i++)
    {
      DrawMesh(meshEnemy, matDefault,
        mono.enemies[i].pos, Quaternion.LookRotation(mono.enemies[i].dir) * mono.enemies[i].rot, 0.015f);
    }

    // if (true)
    // {
    //   DrawMesh(meshSphere, matDebug, Vector3.zero, Quaternion.identity, mono.safeRadius / 2);
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
  void DrawMesh(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, float scale)
  {
    m4.SetTRS(pos, rot.normalized, Vector3.one * scale);
    Graphics.DrawMesh(mesh, m4, mat, 0);
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
  RecorderController m_RecorderController;
  // public RCSettings rcSettings;
  // public IRSettings irSettings;

  public void Start(Monolith mono)
  {
    this.mono = mono;
    RecorderControllerSettingsPreset preset = Resources.Load<RecorderControllerSettingsPreset>("RCSettings");
    RecorderControllerSettings settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
    preset.ApplyTo(settings);
    m_RecorderController = new RecorderController(settings);
    // controllerSettings.AddRecorderSettings(irSettings);
    // RecorderController

    // var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
    // m_RecorderController = new RecorderController(controllerSettings);

    // var mediaOutputFolder = Path.Combine(Application.dataPath, "..", "Screenshots");

    // // Image
    // var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
    // imageRecorder.name = "ScreenCap";
    // imageRecorder.Enabled = true;
    // imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
    // imageRecorder.CaptureAlpha = false;

    // imageRecorder.OutputFile = Path.Combine(mediaOutputFolder, "image_") + DefaultWildcard.Take;

    // imageRecorder.imageInputSettings = 

    // // imageRecorder.
    // imageRecorder.imageInputSettings = new GameViewInputSettings
    // {
    //   OutputWidth = 7680,
    //   OutputHeight = 4320,
    // };

    // // Setup Recording
    // controllerSettings.AddRecorderSettings(imageRecorder);
    // controllerSettings.SetRecordModeToSingleFrame(0);
  }

  public void Update()
  {
    if (mono.rig.rHand.altButton.down)
    {
      m_RecorderController.PrepareRecording();
      m_RecorderController.StartRecording();
    }
  }
}
