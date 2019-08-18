using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapseMgr : MonoBehaviour
{
    static WaveFunctionCollapseMgr Instance;


    public GameObject[] Prefab;
    public float TimeWait = 1;

    enum EDirection
    {
        UP = 0,
        DOWN = 1,
        LEFT = 2,
        RIGHT = 3,
        RIGHT_UP = 4,
        LEFT_DOWN = 5,
        LEFT_UP = 6,
        RIGHT_DOWN = 7,
    }

    struct Position : IEquatable<Position>
    {
        public int x;
        public int y;
        public Position(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(Position other)
        {
            return x == other.x && y == other.y;
        }
    }

    static Position[] NearByPos = new Position[]{
        new Position(0,-1),
        new Position(0,1),
        new Position(-1,0),
        new Position(1,0),
        /*
        new Position(1,-1),
        new Position(-1,1),
        new Position(-1,-1),
        new Position(1,1),*/
    };

    int[,] sampleMap = {
        {0,0,0,4,0,0,0,0},
        {0,0,1,2,4,4,0,0},
        {0,1,2,2,6,2,3,0},
        {1,2,6,2,2,6,2,3},
        {0,1,2,6,2,2,3,0},
        {0,1,2,2,6,2,3,0},
        {4,0,1,2,5,5,0,4},
        {2,3,0,5,0,0,1,2},
    };

    struct CompatibilitiesItem : IEquatable<CompatibilitiesItem>{
        public int CurrentTile;
        public int NextTile;

        public EDirection Direction;

        public CompatibilitiesItem(int CurrentTile, int NextTile, EDirection Direction)
        {
            this.CurrentTile = CurrentTile;
            this.NextTile = NextTile;
            this.Direction = Direction;
        }

        public bool Equals(CompatibilitiesItem other)
        {
            return CurrentTile == other.CurrentTile && NextTile == other.NextTile && Direction == other.Direction;
        }
    }

    class Compatibilities{
        public List<CompatibilitiesItem> compatibilities = new List<CompatibilitiesItem>();

        public bool check(int current, int next, EDirection dir){
            CompatibilitiesItem item = new CompatibilitiesItem(current, next, dir);
            return compatibilities.Contains(item);
        }

    }



    
    (Dictionary<int, int> weight, Compatibilities compatibilities) _parseMap(int[,] sampleMap){
        Dictionary<int, int> weight = new Dictionary<int, int>();
        Compatibilities compatibilities = new Compatibilities();

        int height = sampleMap.GetUpperBound(0) +1 ;
        int width = sampleMap.GetUpperBound(1)  +1; 
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var unit = sampleMap[y,x];
                if (!weight.ContainsKey(unit))
                    weight[unit] = 0; 
                else
                    weight[unit]++; 

                for (int dir = 0; dir < NearByPos.Length; dir++)
                {
                    var newX = x + NearByPos[dir].x;
                    var newY = y + NearByPos[dir].y;
                    if (newY >=0 && newY < height && newX >= 0 && newX < width){
                        var com = new CompatibilitiesItem(){
                            CurrentTile = sampleMap[y,x],
                            NextTile = sampleMap[newY,newX],
                            Direction = (EDirection)dir
                        };
                        if (!compatibilities.compatibilities.Contains(com)){
                            compatibilities.compatibilities.Add(com);
                        }
                    }
                }
            }
        }


        return (weight, compatibilities);
    }


    class WaveFuntion{

        int width, height;
        Dictionary<int, int> weight;

        Dictionary<Position, List<int>> coefficients = new Dictionary<Position, List<int>>();

        public WaveFuntion(int width, int height, Dictionary<int, int> weight)
        {
            this.width = width;
            this.height = height;
            this.weight = weight;


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    coefficients[new Position(x,y)] = new List<int>(weight.Keys);
                }
            }

        }

        public bool isFullyCollapsed(){
            foreach (var kp in coefficients)
            {
                if (kp.Value.Count > 1)
                    return false;
            }
            return true;
        }

        public float Entropy(Position pos){
            float sumWeight=0;
            float sumLogWeight = 0;

            var list = coefficients[pos];
            for (int i = 0; i < list.Count; i++)
            {
                var w = weight[list[i]];
                sumWeight += w;
                sumLogWeight += w * Mathf.Log(w);
            }
            
            return Mathf.Log(sumWeight) - sumLogWeight/sumWeight;
        }


        public List<int> GetTileList(Position pos){
            return coefficients[pos];
        }

        public void Collapse(Position pos){

            var list = coefficients[pos];
            
            Dictionary<int,int> validWeight = new Dictionary<int, int>();

            float totalWeight = 0;
            foreach (var item in weight)
            {
                if (list.Contains(item.Key)){
                     validWeight[item.Key] = item.Value;
                     totalWeight += item.Value;
                }                
            }

            var rnd = UnityEngine.Random.Range(0f, totalWeight);
            
            foreach (var item in validWeight)
            {
                rnd -= item.Value;
                if (rnd < 0)
                {
                    list.Clear();
                    list.Add(item.Key);
                    break;
                }
            }
        }


        public void constrain(Position pos,int removedTile){
            coefficients[pos].Remove(removedTile);
        }

        public int GetCollapsed(Position pos){
            var tileList = GetTileList(pos);
            if (tileList.Count != 1)
                return -1;
            else 
                return tileList[0];
        }

        public int[,] GetAllCollapsed(){
            int[,] output = new int [height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Position p = new Position(x, y);
                    output[y, x] = GetCollapsed(p);
                }
            }
            return output;

        }

    }

    class Model{

        int width;
        int height;

        Dictionary<int, int> weight;

        Compatibilities compatibilities;


        WaveFuntion wavefuntion;
        public Model(int width, int height, Dictionary<int, int> weight, Compatibilities compatibilities)
        {
            this.width = width;
            this.height = height;
            this.weight = weight;
            this.compatibilities = compatibilities;

            wavefuntion = new WaveFuntion(width, height, weight);


            
        }


        public IEnumerator Run(){
            while (!wavefuntion.isFullyCollapsed())
            {
                var minPos = getMinEntropyPos();
                wavefuntion.Collapse(minPos);
                Propagate(minPos);
                

                var map = wavefuntion.GetAllCollapsed();
                /*
                string output = "";        
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        string a="";
                        if (minPos.x == x && minPos.y==y) a="*";
                        var tileList = wavefuntion.GetTileList(new Position(x,y));
                        string str = "";
                        tileList.ForEach((ele)=>{
                            str += ele;
                        });

                        if (map[y,x] < 0)
                            output += a+"-" + "(" +str+")  ";
                        else
                            output += a+map[y,x] + "(" +str+")  ";
                    }
                    output += "\n";
                }

                Debug.Log(output);*/

                


                
                WaveFunctionCollapseMgr.Instance.Draw(map);
                yield return new WaitForSeconds(WaveFunctionCollapseMgr.Instance.TimeWait);
            }

            result =  wavefuntion.GetAllCollapsed();

        }

        public int[,] result;

        Position getMinEntropyPos(){
            float minEnt = float.MaxValue;
            Position minPos = new Position();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Position p = new Position(x,y);
                    var pointList = wavefuntion.GetTileList(p);
                    if (pointList.Count == 1)
                        continue;

                    for (int i = 0; i < pointList.Count; i++)
                    {
                        var ent = wavefuntion.Entropy(p) - UnityEngine.Random.Range(0f,1f)/1000f;
                        if (minEnt == float.MaxValue || ent < minEnt){
                            minEnt = ent;
                            minPos = p;
                        }
                    }
                }
            }
            return minPos;

        }

        void Propagate(Position pos){
            Stack<Position> stack = new Stack<Position>();
            stack.Push(pos);

            while(stack.Count > 0){

                var point = stack.Pop();
                var currentPossibleTile = wavefuntion.GetTileList(point);

                for (int dir = 0; dir < NearByPos.Length; dir++)
                {
                    var newX = point.x + NearByPos[dir].x;
                    var newY = point.y + NearByPos[dir].y;
                    if (newY < 0 || newY >= height || newX < 0 || newX >= width) continue;
                    
                    var otherPoint = new Position(newX, newY);
                    var tileList =  wavefuntion.GetTileList(otherPoint).ToArray();

                    for (int i = 0; i < tileList.Length; i++)
                    {
                        var otherTile = tileList[i];
                        bool anyPossible = false;
                        for (int j = 0; j < currentPossibleTile.Count; j++)
                        {
                            if (compatibilities.check(currentPossibleTile[j], otherTile, (EDirection)dir)){
                                anyPossible = true;
                                break;
                            }
                        }

                        if (!anyPossible)
                        {
                            wavefuntion.constrain(otherPoint, otherTile);
                            stack.Push(otherPoint);
                        }
                    }
                }

            }

            


        }
    }

    public int Width = 5;
    public int Height = 5;

    bool generating = false;
    public GameObject Error;
    public GameObject Succeed;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        generating = true;
        Error.gameObject.SetActive(false);
        Succeed.gameObject.SetActive(false);

        Instance = this;

        int height = Height;
        int width = Width;


        var (weight, compatibilities) = _parseMap(sampleMap);

        Model model = new Model(width, height, weight, compatibilities);
        yield return model.Run();

        int[,] map = model.result;

        string output = "";        
        bool success = true;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                output += map[y,x] + " ";
                if (map[y,x] == -1)
                    success = false;
                /*if (map[y,x] < 0){
                    Debug.Log("Failed");
                    yield return new WaitForSeconds(TimeWait*2);
                    goto Retry;
                }*/
            }
            output += "\n";
        }

        Debug.Log(output);

        Error.gameObject.SetActive(!success);
        Succeed.gameObject.SetActive(success);

        generating = false;
    }


    List<GameObject> go = new List<GameObject>();

    public void Draw(int[,] map){
        for (int i = 0; i < go.Count; i++)
        {
            GameObject.Destroy(go[i]);
        }

        int height = map.GetUpperBound(0) +1 ;
        int width = map.GetUpperBound(1)  +1; 
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var index = map[y,x];  
                if (index >= 0){
                    var g = GameObject.Instantiate(Prefab[index]);
                    g.transform.localPosition = new Vector3((float)x - (float)width/2,0,-(float)y + (float)height/2);
                    go.Add( g );
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!generating && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(Start());
        }
    }
}
