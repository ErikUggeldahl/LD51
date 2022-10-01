using UnityEngine;

public class Spawner : MonoBehaviour
{
    const float SPACING = 2f;
    static Vector3 Y_MASK = new Vector3(1f, 0f, 1f);

    [SerializeField]
    BattleSettings settings;

    [SerializeField]
    Timer timer;

    [SerializeField]
    GameObject soldierPre;

    new Transform camera;

    int[] squadNextIDs;
    Transform[] teamParents;

    Transform debugTarget;

    void Start()
    {
        camera = Camera.main.transform;
        squadNextIDs = new int[settings.teams.Length];

        Transform allUnits = new GameObject("All Units").transform;

        teamParents = new Transform[settings.teams.Length];
        for (var i = 0; i < settings.teams.Length; i++)
        {
            teamParents[i] = new GameObject($"Team {i}").transform;
            teamParents[i].parent = allUnits;
        }

        debugTarget = GameObject.Find("Debug Target").transform;
    }

    void Update()
    {
        bool spawn0 = Input.GetKeyDown(KeyCode.E);
        bool spawn1 = Input.GetKeyDown(KeyCode.R);
        if (spawn0 || spawn1)
        {
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out hit)) return;
            Spawn(spawn0 ? 0 : 1, hit.point, 20, 4);
        }
    }

    void DebugSpawn(int teamID)
    {

    }

    void Spawn(int teamID, Vector3 origin, int count, int rows)
    {
        var squadID = squadNextIDs[teamID];
        Transform squadParent = new GameObject($"Squad {squadID}").transform;
        squadParent.parent = teamParents[teamID];

        var cols = count / rows;
        var row = 0;
        for (int i = 0; i < count; i++)
        {
            var col = i % cols;
            if (col == 0) row++;
            var spawnOffset = origin + camera.right * col * SPACING + Vector3.Scale(camera.forward, Y_MASK) * row * SPACING;

            var soldierGO = Instantiate(soldierPre, spawnOffset, Quaternion.identity, squadParent);
            var soldier = soldierGO.GetComponent<Soldier>();
            soldier.teamID = teamID;
            soldier.squadID = squadID;
            soldier.indexInSquad = i;
            soldier.timer = timer;
            soldier.target = debugTarget;
        }

        squadNextIDs[teamID]++;
    }
}
