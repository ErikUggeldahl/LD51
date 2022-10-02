using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Spawner : MonoBehaviour
{
    const int MAX_SPAWN = 100;
    const float SPACING = 2;
    static readonly Vector3 Y_MASK = new Vector3(1, 0, 1);

    [SerializeField]
    BattleSettings settings;

    [SerializeField]
    Timer timer;

    [SerializeField]
    GameObject markerPre;

    [SerializeField]
    GameObject soldierPre;

    [SerializeField]
    GameObject brickPre;

    [SerializeField]
    Transform brickParent;

    [SerializeField]
    Transform holoBrick;

    [SerializeField]
    Transform[] targetLocations;

    [SerializeField]
    SoldierSprites soldierSprites;

    [SerializeField]
    SoldierSprites archerSprites;

    [Header("UI Connection")]
    [SerializeField]
    ToggleGroup teamToggles;

    [SerializeField]
    ToggleGroup kindToggles;

    [SerializeField]
    TMPro.TMP_InputField countInput;

    [SerializeField]
    TMPro.TMP_InputField rowsInput;

    public static Transform arrowParent;

    new Transform camera;

    int team;
    Soldier.Kind kind;
    int count;
    int rows;
    bool spawning = false;
    bool spawningTarget = false;
    bool spawningBrick = false;

    int[] squadNextIDs;
    Transform[] teamParents;
    Transform markerParent;

    Vector3[] spawnPositions = new Vector3[MAX_SPAWN];

    const float RAY_DISTANCE = 500;
    int SOLDIER_LAYER_MASK;
    int SURFACE_LAYER_MASK;

    void Start()
    {
        arrowParent = GameObject.Find("Arrows").transform;
        camera = Camera.main.transform;
        squadNextIDs = new int[settings.teams.Length];

        SOLDIER_LAYER_MASK = LayerMask.GetMask("Soldier");
        SURFACE_LAYER_MASK = LayerMask.GetMask("Default");

        CreateUnitStore();
        CreateMarkers();
    }

    void CreateUnitStore()
    {
        Transform allUnits = new GameObject("All Units").transform;

        teamParents = new Transform[settings.teams.Length];
        for (var i = 0; i < settings.teams.Length; i++)
        {
            teamParents[i] = new GameObject($"Team {i}").transform;
            teamParents[i].parent = allUnits;
        }
    }

    void CreateMarkers()
    {
        markerParent = new GameObject("Markers").transform;
        markerParent.parent = transform;

        for (var i = 0; i < MAX_SPAWN; i++)
        {
            var marker = Instantiate(markerPre);
            marker.SetActive(false);
            marker.transform.parent = markerParent;
        }

        holoBrick.parent = markerParent.GetChild(0);
    }

    public void OnSpawnClicked()
    {
        if (spawning) return;

        team = int.Parse(teamToggles.ActiveToggles().First().name.Last().ToString());

        if (kindToggles.ActiveToggles().First().name.Contains("Target"))
        {
            count = 1;
            rows = 1;
            spawningTarget = true;
        }
        else if (kindToggles.ActiveToggles().First().name.Contains("Brick"))
        {
            count = 1;
            rows = 1;
            spawningBrick = true;
            holoBrick.gameObject.SetActive(true);
        }
        else
        {
            kind = kindToggles.ActiveToggles().First().name.Contains("Soldier") ? Soldier.Kind.Soldier : Soldier.Kind.Archer;
            count = Mathf.Clamp(int.Parse(countInput.text), 1, MAX_SPAWN);
            rows = Mathf.Clamp(int.Parse(rowsInput.text), 1, MAX_SPAWN);

            countInput.text = count.ToString();
            rowsInput.text = rows.ToString();
        }

        SetupMarkers();
        spawning = true;
    }

    void Update()
    {
        if (spawning)
        {
            PositionMarkers();
            //if (spawningBrick)
            //{
            //    holoBrick.posi
            //}

            if (Input.GetMouseButtonDown(0))
            {
                spawning = false;
                TearDownMarkers();

                if (spawningTarget)
                {
                    spawningTarget = false;
                    targetLocations[team].position = spawnPositions[0];
                }
                else if (spawningBrick)
                {
                    spawningBrick = false;
                    holoBrick.gameObject.SetActive(false);
                    SpawnBrick();
                    
                }
                else
                {
                    Spawn(kind, team, count);
                }
            }
        }


        var setup0 = Input.GetKeyDown(KeyCode.E);
        var setup1 = Input.GetKeyDown(KeyCode.R);
        var setup2 = Input.GetKeyDown(KeyCode.T);
        if (setup0 || setup1 || setup2)
        {
            SetupMarkers();
        }

        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.T))
        {
            PositionMarkers();
        }

        var spawn0 = Input.GetKeyUp(KeyCode.E);
        var spawn1 = Input.GetKeyUp(KeyCode.R);
        var spawn2 = Input.GetKeyUp(KeyCode.T);
        if (spawn0 || spawn1 || spawn2)
        {
            TearDownMarkers();
            DebugSpawn(spawn2 ? Soldier.Kind.Archer : Soldier.Kind.Soldier, spawn0 || spawn2 ? 0 : 1);
        }

        if (Input.GetKey(KeyCode.Z))
        {
            DebugKill();
        }
    }

    void SetupMarkers()
    {
        var color = settings.teams[team].color;

        for (int i = 0; i < count; i++)
        {
            var marker = markerParent.GetChild(i);
            marker.gameObject.SetActive(true);
            marker.GetComponentInChildren<Renderer>().material.color = color;
        }
    }

    void PositionMarkers()
    {
        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit, RAY_DISTANCE, SURFACE_LAYER_MASK)) return;

        CalculateSpawnPositions(spawnPositions, hit.point, count, rows);

        for (var i = 0; i < count; i++)
        {
            markerParent.GetChild(i).position = spawnPositions[i];
        }
    }

    void TearDownMarkers()
    {
        for (int i = 0; i < count; i++)
        {
            var marker = markerParent.GetChild(i);
            marker.gameObject.SetActive(false);
        }
    }

    void DebugKill()
    {
        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit, RAY_DISTANCE, SOLDIER_LAYER_MASK)) return;

        var soldier = hit.collider.GetComponent<Soldier>();
        if (soldier == null) return;

        soldier.EnterDeath();
    }

    void DebugSpawn(Soldier.Kind kind, int teamID)
    {
        Spawn(kind, teamID, 20);
    }

    void CalculateSpawnPositions(in Vector3[] positions, Vector3 origin, int count, int rows)
    {
        var cols = count / rows;
        var row = -1;

        for (int i = 0; i < count; i++)
        {
            var col = i % cols;
            if (col == 0) row++;
            positions[i] = origin + camera.right * col * SPACING - Vector3.Scale(camera.forward, Y_MASK) * row * SPACING;
        }
    }

    SoldierSprites KindToSprites(Soldier.Kind kind)
    {
        switch (kind)
        {
            case Soldier.Kind.Soldier: return soldierSprites;
            case Soldier.Kind.Archer: return archerSprites;
        }
        return null;
    }

    void Spawn(Soldier.Kind kind, int teamID, int count)
    {
        var squadID = squadNextIDs[teamID];
        var squadParent = new GameObject($"Squad {squadID}", typeof(Squad)).transform;
        squadParent.parent = teamParents[teamID];

        var squad = squadParent.GetComponent<Squad>().Create(kind, count, teamID, squadID, targetLocations[teamID]);

        for (int i = 0; i < count; i++)
        {
            var soldierGO = Instantiate(soldierPre, spawnPositions[i], Quaternion.identity, squadParent);
            var soldier = soldierGO.GetComponent<Soldier>();
            soldier.timer = timer;
            soldier.kind = kind;
            soldier.sprites = KindToSprites(kind);
            squad.AddSoldier(soldier);
        }

        squadNextIDs[teamID]++;
    }

    void SpawnBrick()
    {
        var brickGO = Instantiate(brickPre, spawnPositions[0], Quaternion.identity, brickParent);
        brickGO.GetComponent<TeamColor>().team = team;
    }
}
