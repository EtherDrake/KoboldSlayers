using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Threading.Tasks;
using TMPro;

public class TurnBasedController : MonoBehaviour
{

    #region Objects
    
    #region Tiles
    public Tilemap Tilemap;
    public Tilemap HighlightTilemap;
    public Tilemap Walkable;

    public Tile HighlightTile;
    public Tile HighlightMoveTile;
    public Tile HighlightAttackTile;
    public Tile HighlightAllyTile;
    public Tile UnavailableTargetTile;

    public Tile DefaultWalakble;
    public Tile MediumWalkable;
    public Tile DifficultWalkable;
    #endregion

    #region UI
    public GameObject MovementBar;
    public GameObject HpBar;   
    private Slider movementBarSlider;   
    private Slider hpBarSlider;
    public Button AbilityButton1;
    public Button AbilityButton2;
    public Button AbilityButton3;
    public Button AbilityButton4;
    public static AbilityProperties abilityProperties1;
    public static AbilityProperties abilityProperties2;
    public static AbilityProperties abilityProperties3;
    public static AbilityProperties abilityProperties4;
    public Sprite PotionCharges0;
    public Sprite PotionCharges1;
    public Sprite PotionCharges2;
    public Sprite PotionCharges3;
    public Image PotionImage;
    public GameObject PauseMenu;
    public Button SwitchAttackTypeButton;

    public Image Portrait;
    #endregion
    
    #endregion    

    #region Navigation
    private Astar _astar;
    private BoundsInt _bounds;
    private GridLayout _grid;
    private Vector3Int[,] _gridValues;
    private int[,] _walkableValues;
    private Vector3 _targetPosition;
    private Vector3Int _targetTile;
    private bool _isMoving;
    private bool _isMovingToAttack;
    private List<Spot> _route;
    private List<Vector3Int> _availablePositions;
    private List<Vector3Int> _enemyPositions;
    public List<Vector3Int> _excludedPositions;
    private int _currentStep;
    public int Speed;
    #endregion

    #region Combat
    private bool _targetMode;
    private Vector3Int _lastHoveredTile;
    private AbilityProperties _activatedAbilityProperties;
    private List<Vector3Int> _selectedTargets;
    public GameObject KoboldPrefab;
    public GameObject VelociriderPrefab;
    public GameObject VelociraptorPrefab;
    public GameObject GoblinKingPrefab;
    #endregion


    #region CharactersQueue
    public List<BaseCharacterController> _characters;
    private BaseCharacterController _currentCharacter;
    private int _currentCharacterIndex = -1;
    private string _enemyTeamTag;
    private int currentTurn = 0;
    private bool isInit;
    private bool isBossPhase;
    public bool isAiTurn;
    public bool isPaused;
    private int phaseNumber = 1;
    #endregion

    #region SceneTransitions
    public Animator FadingAnimator;
    public Image FadeImage;
    #endregion

    #region  Audio   
    public AudioClip PotionDrink;
    #endregion


    // Start is called before the first frame update
    async void Start()
    {
        _characters = GameObject.FindGameObjectsWithTag("Team_A").Select(x =>  x.GetComponent<BaseCharacterController>()).ToList();
        _characters.AddRange(GameObject.FindGameObjectsWithTag("Team_B").Select(x =>  x.GetComponent<BaseCharacterController>()));        

        movementBarSlider = MovementBar.GetComponent<Slider>();
        hpBarSlider = HpBar.GetComponent<Slider>();
        _selectedTargets = new List<Vector3Int>();

        Tilemap.CompressBounds();
        _bounds = Tilemap.cellBounds;
        _grid = Tilemap.layoutGrid;

        _astar = new Astar(_bounds.size.x, _bounds.size.y);
        _gridValues = CreateGrid();
        _walkableValues = CreateGridValues();        
        _availablePositions = new List<Vector3Int>();

        foreach(var character in _characters)
        {
            character.GetPosition(Tilemap);
        }

        await passTurn();
    }

    // Update is called once per frame
    async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(Time.timeScale > 0f)
            {
                Time.timeScale = 0f;
                isPaused = true;
                PauseMenu.SetActive(true);
            }
            else
            {
                Time.timeScale = 1f;
                isPaused = false;
                PauseMenu.SetActive(false);
            }
        }

        if (_isMoving && !_isMovingToAttack)
            MovePlayer();
        else
        {
            if(EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if(!isAiTurn)
            {
                if (Input.GetKeyDown(KeyCode.Space) && !_isMovingToAttack && !isPaused)
                {
                    await passTurn();
                }
                if (Input.GetMouseButtonDown(0) && !isPaused)
                {
                    var worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var currentTile = Tilemap.WorldToCell(_currentCharacter.transform.position);
                    var targetTile = Tilemap.WorldToCell(worldPosition);

                    if(_activatedAbilityProperties != null && HighlightTilemap.HasTile(targetTile))
                    {
                        _selectedTargets.Add(targetTile);
                        if(_activatedAbilityProperties.NumberOfTargets <= _selectedTargets.Count)
                        {
                            var targetTag = _activatedAbilityProperties.TargetsEnemies ? _enemyTeamTag : "Team_A";

                            switch(_activatedAbilityProperties.SkillNumber)
                            {
                                case 1:
                                    StartCoroutine(_currentCharacter.Skill1(Tilemap, _selectedTargets, transform.position, _enemyTeamTag));
                                    abilityProperties1.Cooldown = 1;
                                    break; 
                                case 2:
                                    StartCoroutine(_currentCharacter.Skill2(Tilemap, _selectedTargets, transform.position, targetTag));
                                    abilityProperties2.Cooldown = 2;
                                    break;   
                                case 3:
                                    StartCoroutine(_currentCharacter.Skill3(Tilemap, _selectedTargets, transform.position, targetTag));
                                    abilityProperties3.Cooldown = 3;
                                    break;     
                                case 4:
                                    StartCoroutine(_currentCharacter.Skill4(Tilemap, _selectedTargets, transform.position, targetTag));
                                    break;                
                            }
                            _activatedAbilityProperties = null;

                            var teamA = GameObject.FindGameObjectsWithTag("Team_A").Select(x =>  x.GetComponent<BaseCharacterController>()).Where(x => x.IsAlive).ToList();
                            var teamB = GameObject.FindGameObjectsWithTag("Team_B").Select(x =>  x.GetComponent<BaseCharacterController>()).Where(x => x.IsAlive).ToList();
                            var missingCharacters = teamA.Where(x => !_characters.Contains(x)).ToList();
                            missingCharacters.AddRange(teamB.Where(x => !_characters.Contains(x)));
                            
                            foreach(var missingCharacter in missingCharacters)
                            {
                                missingCharacter.GetPosition(Tilemap);
                                _characters.Add(missingCharacter);
                            } 

                            getMoveRange();
                            unsetButtons();
                            updateCooldowns();
                        }

                        updateUI();
                    }                    
                    else
                    {
                        if(_currentCharacter.Actions > 0 && _enemyPositions.Any(e => e == targetTile))
                        {
                            var autoAttackProperties = _currentCharacter.AutoAttackProperties;
                            var targetEnemy = GameObject.FindGameObjectsWithTag(_enemyTeamTag).Select(x =>  x.GetComponent<BaseCharacterController>()).FirstOrDefault(e => e.Position.Contains(targetTile) && e.IsAlive);
                            var targetEnemyTile = Tilemap.WorldToCell(targetEnemy.transform.position);

                            var targetEnenmyTiles = targetEnemy.Position;
                            if(autoAttackProperties.IsRanged)
                            {
                                var minDistanceX = targetEnenmyTiles.Min(x => (Mathf.Abs(x.x - currentTile.x)));
                                var minDistanceY = targetEnenmyTiles.Min(x => (Mathf.Abs(x.y - currentTile.y)));

                                if(minDistanceX < autoAttackProperties.Range && minDistanceY < autoAttackProperties.Range)
                                {
                                    StartCoroutine(_currentCharacter.AutoAttack(Tilemap, targetEnenmyTiles.FirstOrDefault(), transform.position, _enemyTeamTag));                                    
                                }
                            }
                            else
                            {
                                var availablePositions = _availablePositions;
                                availablePositions.Insert(0, currentTile);
                                var closestTileToEnemy = availablePositions.FirstOrDefault(t => targetEnemy.Position.Any(p => Mathf.Abs(p.x - t.x) <= autoAttackProperties.Range && Mathf.Abs(p.y - t.y) <= autoAttackProperties.Range));
                                
                                if(closestTileToEnemy != null)
                                {
                                    if(targetEnemy.Position.Any(p => Mathf.Abs(p.x - closestTileToEnemy.x) <= autoAttackProperties.Range && Mathf.Abs(p.y - closestTileToEnemy.y) <= autoAttackProperties.Range))
                                    {
                                        if(closestTileToEnemy != currentTile)
                                        {                                    
                                            StartCoroutine(WalkTo(closestTileToEnemy, () => {
                                                StartCoroutine(_currentCharacter.AutoAttack(Tilemap, targetTile, closestTileToEnemy, _enemyTeamTag));
                                                getMoveRange();
                                            }));
                                        }
                                        else
                                        {                                    
                                            StartCoroutine(_currentCharacter.AutoAttack(Tilemap, targetTile, closestTileToEnemy, _enemyTeamTag));
                                        }
                                    }
                                }
                            }

                            getMoveRange();
                        }
                        else
                        {
                            SetTarget(worldPosition);
                        }
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Alpha1) && !isPaused)
                {
                    ActivateAbility1();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) && !isPaused)
                {
                    ActivateAbility2();
                }                
                else if (Input.GetKeyDown(KeyCode.Alpha3) && !isPaused)
                {
                    ActivateAbility3();
                }                 
                else if (Input.GetKeyDown(KeyCode.Alpha4) && !isPaused)
                {
                    ActivateAbility4();
                }                  
            }

            if(_activatedAbilityProperties != null)
            {
                if(_activatedAbilityProperties.IsAOE)
                {                    
                    var worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var hoveredTile = Tilemap.WorldToCell(worldPosition);
                    var currentTile = Tilemap.WorldToCell(_currentCharacter.transform.position);

                    if(hoveredTile != _lastHoveredTile)                
                    {
                        if(_lastHoveredTile != null)
                        {
                            highlightAOE(_lastHoveredTile, null, 1);
                        }
                        if(Mathf.Abs(hoveredTile.x - currentTile.x) < _activatedAbilityProperties.Range && Mathf.Abs(hoveredTile.y - currentTile.y) < _activatedAbilityProperties.Range)
                        {
                            highlightAOE(hoveredTile, HighlightAttackTile, _activatedAbilityProperties.Radius);
                        }
                        else
                        {
                            highlightAOE(hoveredTile, UnavailableTargetTile, _activatedAbilityProperties.Radius);
                        }
                        _lastHoveredTile = hoveredTile;
                    }
                }
            }        
        }
    }

    public async void PassTurnWrapper()
    {
        if(!isAiTurn && !isPaused)
        {
            await passTurn();
        }
    }

    private async Task passTurn()
    {        
        unsetButtons();
        _activatedAbilityProperties = null;
        _selectedTargets.Clear();  
        _characters.RemoveAll(x => x == null);

        

        var teamA = GameObject.FindGameObjectsWithTag("Team_A").Select(x =>  x.GetComponent<BaseCharacterController>()).Where(x => x.IsAlive).ToList();
        var teamB = GameObject.FindGameObjectsWithTag("Team_B").Select(x =>  x.GetComponent<BaseCharacterController>()).Where(x => x.IsAlive).ToList();
        var missingCharacters = teamA.Where(x => !_characters.Contains(x)).ToList();
        missingCharacters.AddRange(teamB.Where(x => !_characters.Contains(x)));

        foreach(var missingCharacter in missingCharacters)
        {
            missingCharacter.GetPosition(Tilemap);
            _characters.Add(missingCharacter);
        } 
             
        
        _currentCharacterIndex++;
        

        if(_characters.Where(x => x.tag == "Team_A").Where(x => x.HP > 0).Count() == 0)
        {
            StartCoroutine(FadeOut(2));
            return;
        }

        if(_currentCharacterIndex > _characters.Count-1 || !isInit)
        {
            _currentCharacterIndex = 0;

            foreach(var character in _characters.Where(x => !x.IsAlive || x.HP <= 0))
            {
                character.GetComponent<Animator>().SetBool("Dead", true);
            }

            _characters.RemoveAll(x => !x.IsAlive || x.HP <= 0);

            currentTurn++;
            ScoreboardController.TurnUpdate.Invoke(currentTurn);
            
            if(_characters.Where(x => x.tag == "Team_B").Count() == 0 && isBossPhase)
            {
                if(phaseNumber == 2)
                {
                    StartCoroutine(FadeOut(3));
                    return;
                }
                else
                {
                    phaseNumber++;
                    isBossPhase = false;
                }
            }

            if(_characters.Where(x => x.tag == "Team_B").Count() <= 2 && !isBossPhase)
            {                
                var numberOfEnemies = UnityEngine.Random.Range(1, 4);
                for(int i = 0; i < numberOfEnemies; i++)
                {
                    BaseCharacterController newEnemy;

                    var character = _characters[UnityEngine.Random.Range(0, _characters.Count())];                    
                    var tile = Tilemap.WorldToCell(character.transform.position);

                    var newX = UnityEngine.Random.Range(tile.x - 5, tile.x + 5);
                    var newY = UnityEngine.Random.Range(tile.y - 5, tile.y + 5);

                    while(_characters.Any(x => (Tilemap.WorldToCell(x.transform.position).x == newX && Tilemap.WorldToCell(x.transform.position).y == newY)) || !Walkable.HasTile(new Vector3Int(newX, newY, 0)))
                    {
                        newX = UnityEngine.Random.Range(tile.x - 5, tile.x + 5);
                        newY = UnityEngine.Random.Range(tile.y - 5, tile.y + 5);
                    }

                    newEnemy = Instantiate(KoboldPrefab, FindTargetPositionFromTile(new Vector3Int(newX, newY, 0), 1), Quaternion.identity).GetComponent<BaseCharacterController>();

                    newEnemy.GetPosition(Tilemap);
                    
                    _characters.Add(newEnemy);
                }                
            }

            if(ScoreboardController.Score > (1000 * phaseNumber) + (phaseNumber * 250) && !isBossPhase)
            {
                if(phaseNumber == 1)
                {
                    var character = _characters[UnityEngine.Random.Range(0, _characters.Count())];                    
                    var tile = Tilemap.WorldToCell(character.transform.position);

                    var newX = UnityEngine.Random.Range(tile.x - 5, tile.x + 5);
                    var newY = UnityEngine.Random.Range(tile.y - 5, tile.y + 5);

                    while(_characters.Any(x => (Tilemap.WorldToCell(x.transform.position).x == newX && Tilemap.WorldToCell(x.transform.position).y == newY)) || !Walkable.HasTile(new Vector3Int(newX, newY, 0)))
                    {
                        newX = UnityEngine.Random.Range(tile.x - 5, tile.x + 5);
                        newY = UnityEngine.Random.Range(tile.y - 5, tile.y + 5);
                    }

                    var boss = Instantiate(VelociriderPrefab, FindTargetPositionFromTile(new Vector3Int(newX, newY, 0), 2), Quaternion.identity).GetComponent<BaseCharacterController>();

                    boss.GetPosition(Tilemap);
                    _characters.Add(boss);
                }
                else
                {
                    var character = _characters[UnityEngine.Random.Range(0, _characters.Count())];                    
                    var tile = Tilemap.WorldToCell(character.transform.position);

                    var newX = UnityEngine.Random.Range(tile.x - 5, tile.x + 5);
                    var newY = UnityEngine.Random.Range(tile.y - 5, tile.y + 5);

                    while(_characters.Any(x => (Tilemap.WorldToCell(x.transform.position).x == newX && Tilemap.WorldToCell(x.transform.position).y == newY)) || !Walkable.HasTile(new Vector3Int(newX, newY, 0)))
                    {
                        newX = UnityEngine.Random.Range(tile.x - 5, tile.x + 5);
                        newY = UnityEngine.Random.Range(tile.y - 5, tile.y + 5);
                    }

                    var boss = Instantiate(GoblinKingPrefab, FindTargetPositionFromTile(new Vector3Int(newX, newY, 0), 1), Quaternion.identity).GetComponent<BaseCharacterController>();

                    boss.GetPosition(Tilemap);
                    _characters.Add(boss);
                }

                isBossPhase = true;
            }

            isInit = true;
        }
        _currentCharacter = _characters[_currentCharacterIndex];
        if(_currentCharacter.IsAlive)
        {  
            _enemyTeamTag = _currentCharacter.tag == "Team_A" ? "Team_B" : "Team_A";

            if(currentTurn % 3 == 0)
            {
                _currentCharacter.potionCharges = Mathf.Clamp(_currentCharacter.potionCharges + 1, 0, 3);
            }

            _currentCharacter.Speed = _currentCharacter.BaseSpeed;
            _currentCharacter.Actions = 1;       
            updateUI();

            Portrait.sprite = _currentCharacter.Portrait;

            if(_currentCharacter.Skill1Properties.Name == null)
            {
                _currentCharacter.InitAbilites();
            }
            
            _currentCharacter.StartTurn();     

            abilityProperties1 = _currentCharacter.Skill1Properties;
            abilityProperties2 = _currentCharacter.Skill2Properties;
            abilityProperties3 = _currentCharacter.Skill3Properties;  
            abilityProperties4 = _currentCharacter.Skill4Properties;             

            _targetMode = false;
            getMoveRange();

            if(_currentCharacter.tag =="Team_B" || _currentCharacter.IsAiControlled)
            {
                isAiTurn = true;
                var enemies = GameObject.FindGameObjectsWithTag(_enemyTeamTag).Select(x => x.GetComponent<BaseCharacterController>()).Where(x => x.IsAlive).ToList();
                if(enemies.Count > 0)
                {
                    var aiDecision = await _currentCharacter.GetComponent<EnemyController>().TakeTurn(Tilemap, _availablePositions, enemies, _astar, _gridValues, _walkableValues);                
                    StartCoroutine(WalkTo(aiDecision.PositionToAttack, async () => {
                        if(aiDecision.IsInRange)
                        {
                            StartCoroutine(_currentCharacter.AutoAttack(Tilemap, aiDecision.EnemyPosition, aiDecision.PositionToAttack, _enemyTeamTag));
                        }
                        this.isAiTurn = false;
                        await passTurn();
                    }));            
                }
                else
                {                    
                    await passTurn();
                }
            }
            else
            {   
                isAiTurn = false;             
                updateCooldowns();
            }
        }
        else
        {
            await passTurn();
        }
    }

    private void updateUI()
    {            
            movementBarSlider.maxValue = _currentCharacter.BaseSpeed;
            movementBarSlider.value = _currentCharacter.Speed;
            
            hpBarSlider.maxValue = _currentCharacter.MaxHP;
            hpBarSlider.value = _currentCharacter.HP;

            MovementBar.GetComponentInChildren<TextMeshProUGUI>().text = $"{_currentCharacter.Speed} / {_currentCharacter.BaseSpeed}";
            HpBar.GetComponentInChildren<TextMeshProUGUI>().text = $"{_currentCharacter.HP} / {_currentCharacter.MaxHP}";

            switch(_currentCharacter.potionCharges)
            {
                case 0: PotionImage.sprite = PotionCharges0; break;
                case 1: PotionImage.sprite = PotionCharges1; break;
                case 2: PotionImage.sprite = PotionCharges2; break;
                case 3: PotionImage.sprite = PotionCharges3; break;
            }
    }

    
    private Vector3Int[,] CreateGrid()
    {
        var spots = new Vector3Int[_bounds.size.x, _bounds.size.y];
        for (int x = _bounds.xMin, i = 0; i < (_bounds.size.x); x++, i++)
        {
            for (int y = _bounds.yMin, j = 0; j < (_bounds.size.y); y++, j++)
            {
                if (Tilemap.HasTile(new Vector3Int(x, y, 0)))
                {
                    spots[i, j] = new Vector3Int(x, y, 0);
                }
                else
                {
                    spots[i, j] = new Vector3Int(x, y, 1);
                }
            }
        }

        return spots;
    }


    private int[,] CreateGridValues()
    {
        var spots = new int[_bounds.size.x, _bounds.size.y];
        for (int x = _bounds.xMin, i = 0; i < (_bounds.size.x); x++, i++)
        {
            for (int y = _bounds.yMin, j = 0; j < (_bounds.size.y); y++, j++)
            {
                if (Walkable.HasTile(new Vector3Int(x, y, 0)))
                {
                    var tile = Walkable.GetTile(new Vector3Int(x, y, 0));
                    if (tile == DefaultWalakble)
                    {
                        spots[i, j] = 0;
                    }
                    else
                    {
                        spots[i, j] = 1;
                    }
                }
                else
                {
                    spots[i, j] = 1000;
                }
            }
        }

        return spots;
    }

    private Vector3 FindTargetPositionFromTile(Vector3Int targetTile, int size)
    {
        float xKoef = 0;
        float yKoef = 0;
        switch(size)
        {
            case 1: xKoef = 0.5f; yKoef = 0.75f; break;
            case 2: xKoef = 0f; yKoef = 1.25f; break;
        }

        var targetTilePosition = _grid.CellToWorld(targetTile);
        return new Vector3(targetTilePosition.x + xKoef, targetTilePosition.y + yKoef, 0f);
    }    

    private void getMoveRange()    
    {
        clearHighlights();
        _availablePositions.Clear();

        _enemyPositions = GameObject.FindGameObjectsWithTag(_enemyTeamTag).Where(e => e.GetComponent<BaseCharacterController>().IsAlive).SelectMany(e =>  e.GetComponent<BaseCharacterController>().Position).ToList();

        _excludedPositions = _characters.Where(x => x.IsAlive && x != _currentCharacter).SelectMany(x => x.Position).ToList();

        var currentTile = Tilemap.WorldToCell(_currentCharacter.transform.position);

        getDistanceToNeighbors(currentTile, currentTile.x + 1, currentTile.y, 1);
        getDistanceToNeighbors(currentTile, currentTile.x - 1, currentTile.y, -1);
        getDistanceToNeighbors(currentTile, currentTile.x, currentTile.y + 1, 2);
        getDistanceToNeighbors(currentTile, currentTile.x, currentTile.y - 1, -2);

        if(_currentCharacter.tag =="Team_A" && !(_currentCharacter is EnemyController))
        {
            foreach(var tile in _availablePositions)
            {
                HighlightTilemap.SetTile(tile, HighlightMoveTile);
            }
        }
    }

    private void getDistanceToNeighbors(Vector3Int currentTile, int x, int y, int source)
    {
        var route = _astar.CreatePath(_gridValues, _walkableValues, new Vector2Int(x, y), new Vector2Int(currentTile.x, currentTile.y), 1000, _excludedPositions);
        if (route != null && route.Skip(1).Sum(step => step.difficulty == 0 ? 1 : 2) <= _currentCharacter.Speed)
        {
            var tile = new Vector3Int(x, y, 0);

            if (Walkable.HasTile(tile) && !_excludedPositions.Contains(tile))
            {
                _availablePositions.Add(tile);

                if (source != -1 && !_availablePositions.Any(pos => pos.x == x + 1 && pos.y == y))
                {
                    getDistanceToNeighbors(currentTile, x + 1, y, 1);
                }

                if (source != 1 && !_availablePositions.Any(pos => pos.x == x - 1 && pos.y == y))
                {
                    getDistanceToNeighbors(currentTile, x - 1, y, -1);
                }

                if (source != -2 && !_availablePositions.Any(pos => pos.x == x && pos.y == y + 1))
                {
                    getDistanceToNeighbors(currentTile, x, y + 1, 2);
                }

                if (source != 2 && !_availablePositions.Any(pos => pos.x == x && pos.y == y - 1))
                {
                    getDistanceToNeighbors(currentTile, x, y - 1, -2);
                }
            }
        }
    }

    
    private void SetTarget(Vector3 position)
    {
        var currentTile = _currentCharacter.Position.FirstOrDefault();
        var targetTile = Tilemap.WorldToCell(position);

        if(_availablePositions.Contains(targetTile))
        {
            _isMoving = true;
            _currentCharacter.Animator.SetBool("IsMoving", _isMoving);


            HighlightTilemap.SetTile(_targetTile, null);
            clearHighlights();

            _targetTile = targetTile;

            _route = _astar.CreatePath(_gridValues, _walkableValues, new Vector2Int(targetTile.x, targetTile.y), new Vector2Int(currentTile.x, currentTile.y), 1000, _enemyPositions);

            _currentStep = 0;
            if(_route != null && _route.Count > 0)
            {
                _targetPosition = FindTargetPositionFromTile(new Vector3Int(_route[_currentStep].X, _route[_currentStep].Y, 0), _currentCharacter.Size);

                HighlightTilemap.SetTile(_targetTile, HighlightTile);
            }
            else
            {
                _targetPosition = currentTile;
            }
        }
    }

    private void MovePlayer()
    {
        _currentCharacter.transform.position = Vector3.MoveTowards(_currentCharacter.transform.position, _targetPosition, Speed * Time.deltaTime);
        if (_currentCharacter.transform.position == _targetPosition)
        {
            _currentStep++;
            if (_route != null && _currentStep < _route.Count)
            {
                _targetPosition = FindTargetPositionFromTile(new Vector3Int(_route[_currentStep].X, _route[_currentStep].Y, 0), _currentCharacter.Size);
                if ((_targetPosition.x < _currentCharacter.transform.position.x && _currentCharacter.FacingRight) 
                    || (_targetPosition.x > _currentCharacter.transform.position.x && !_currentCharacter.FacingRight))
                {
                    _currentCharacter.Flip();
                }
            }
            else
            {
                _isMoving = false;
                _currentCharacter.Animator.SetBool("IsMoving", _isMoving);
                HighlightTilemap.SetTile(_targetTile, null);
                
                if(_route != null)
                {
                    var distance = _route.Skip(1).Sum(step => step.difficulty == 0 ? 1 : 2);
                    _currentCharacter.Speed -= distance;
                    movementBarSlider.value = _currentCharacter.Speed;
                    updateUI();
                }
                _currentCharacter.GetPosition(Tilemap);
                
                getMoveRange();
            }
        }
    }

    private IEnumerator WalkTo(Vector3Int tile, Action doOnArrival)
    {
        _isMovingToAttack = true;        
        SetTarget(Tilemap.CellToWorld(tile));
        while(_isMoving)
        {
            MovePlayer();
            yield return new WaitForEndOfFrame();
        }
        _isMovingToAttack = false;
        doOnArrival();
    }

    private void clearHighlights()
    {
        foreach(var cell in _gridValues)
        {
            HighlightTilemap.SetTile(cell, null);
        }
    }

    private void highlightAOE(Vector3Int centerTile, Tile markTile, int radius)
    {
        HighlightTilemap.SetTile(centerTile, markTile);  

        for(int i = 1; i <= radius; i++)
        {                        
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x + i, centerTile.y, centerTile.z), markTile);
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x - i, centerTile.y, centerTile.z), markTile);
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x, centerTile.y + i, centerTile.z), markTile);
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x, centerTile.y - i, centerTile.z), markTile);
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x + i, centerTile.y + i, centerTile.z), markTile);
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x - i, centerTile.y - i, centerTile.z), markTile);
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x + i, centerTile.y - i, centerTile.z), markTile);
            HighlightTilemap.SetTile(new Vector3Int(centerTile.x - i, centerTile.y + i, centerTile.z), markTile);
        }
    }    

    public void SwitchMode()
    {
        _targetMode = !_targetMode;
        clearHighlights();
        if(_targetMode)
        {
            if(_currentCharacter.Actions > 0)
            {
                AbilityButton1.GetComponent<Image>().color = new Color32(255, 248, 171, 255);
            }
            else
            {
                _targetMode = !_targetMode;
            }
        }
        else
        {
            getMoveRange();
            AbilityButton1.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        }

    }

    private void ActivateAbility(AbilityProperties abilityProperties)
    {       
        _activatedAbilityProperties = abilityProperties;      
        clearHighlights();

        if(abilityProperties.IsRanged)
        {
            if(!abilityProperties.IsAOE)
            {
                var availableTargets = new List<Vector3Int>();
                var availableTargetCharacters = new List<BaseCharacterController>();
                if(abilityProperties.TargetsEnemies)
                {   
                    if(abilityProperties.TargetsDead)
                    {   
                        var charctersPositions = _characters.Where(x => x.IsAlive).SelectMany(x => x.Position);
                        availableTargetCharacters = GameObject.FindGameObjectsWithTag(_enemyTeamTag).Select(e => e.GetComponent<BaseCharacterController>()).Where(e => e.IsAlive == false && e.Size < _currentCharacter.HP && !charctersPositions.Contains(e.Position[0])).ToList();                        
                    }
                    else
                    {
                        availableTargetCharacters = GameObject.FindGameObjectsWithTag(_enemyTeamTag).Select(e => e.GetComponent<BaseCharacterController>()).Where(e => e.IsAlive == true).ToList();                        
                    }
                }
                else
                {                    
                    availableTargetCharacters = GameObject.FindGameObjectsWithTag("Team_A").Select(e => e.GetComponent<BaseCharacterController>()).Where(e => e.IsAlive).ToList();
                }

                var currentTile = Tilemap.WorldToCell(_currentCharacter.transform.position);

                foreach(var character in availableTargetCharacters)
                {
                    if(character.Position.Any(x => Mathf.Abs(x.x - currentTile.x) < _activatedAbilityProperties.Range && Mathf.Abs(x.y - currentTile.y) < _activatedAbilityProperties.Range))
                    {
                        availableTargets.AddRange(character.Position);
                    }
                }                

                foreach(var cell in availableTargets)
                {
                    HighlightTilemap.SetTile(cell, abilityProperties.TargetsEnemies ? HighlightAttackTile : HighlightAllyTile);
                }
            }
        }
        else
        {
            if(abilityProperties.IsInstant)
            {
                var targetTiles = new List<Vector3Int>();
                var centerTile = Tilemap.WorldToCell(_currentCharacter.transform.position);
                targetTiles.Add(centerTile);

                for(int i = 1; i <= abilityProperties.Radius; i++)
                {                        
                    targetTiles.Add(new Vector3Int(centerTile.x + i, centerTile.y, centerTile.z));
                    targetTiles.Add(new Vector3Int(centerTile.x - i, centerTile.y, centerTile.z));
                    targetTiles.Add(new Vector3Int(centerTile.x, centerTile.y + i, centerTile.z));
                    targetTiles.Add(new Vector3Int(centerTile.x, centerTile.y - i, centerTile.z));
                    targetTiles.Add(new Vector3Int(centerTile.x + i, centerTile.y + i, centerTile.z));
                    targetTiles.Add(new Vector3Int(centerTile.x - i, centerTile.y - i, centerTile.z));
                    targetTiles.Add(new Vector3Int(centerTile.x + i, centerTile.y - i, centerTile.z));
                    targetTiles.Add(new Vector3Int(centerTile.x - i, centerTile.y + i, centerTile.z));
                }

                var targetTag = abilityProperties.TargetsEnemies ? _enemyTeamTag : "Team_A";

                switch(abilityProperties.SkillNumber)
                {
                    case 1: StartCoroutine(_currentCharacter.Skill1(Tilemap, targetTiles, transform.position, targetTag)); break;
                    case 2: StartCoroutine(_currentCharacter.Skill2(Tilemap, targetTiles, transform.position, targetTag)); break;
                    case 3: StartCoroutine(_currentCharacter.Skill3(Tilemap, targetTiles, transform.position, targetTag)); break;
                    case 4: StartCoroutine(_currentCharacter.Skill4(Tilemap, targetTiles, transform.position, targetTag)); break;
                }
                _activatedAbilityProperties = null;                
                getMoveRange();
                updateUI();
                updateCooldowns();
            }
        }
    }
    
    private void unsetButtons()
    {
        AbilityButton1.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        AbilityButton2.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        AbilityButton3.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        AbilityButton4.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
    }

    private void updateCooldowns()
    {


        
        var abilityButtonImage = AbilityButton1.GetComponentsInChildren<Image>()[2];
        var abilityLockImage = AbilityButton1.GetComponentsInChildren<Image>()[1];
        var abilityLockText = abilityLockImage.GetComponentsInChildren<TextMeshProUGUI>()[0];
        abilityButtonImage.sprite = _currentCharacter.Ability1Icon;
        abilityLockImage.enabled = abilityProperties1.Cooldown > 0;
        abilityLockText.enabled = abilityProperties1.Cooldown > 0;
        abilityLockText.text = abilityProperties1.Cooldown.ToString();
        abilityButtonImage.enabled = !abilityLockImage.enabled;
                
        abilityButtonImage = AbilityButton2.GetComponentsInChildren<Image>()[2];
        abilityLockImage = AbilityButton2.GetComponentsInChildren<Image>()[1];
        abilityLockText = abilityLockImage.GetComponentsInChildren<TextMeshProUGUI>()[0];
        abilityButtonImage.sprite = _currentCharacter.Ability2Icon;
        abilityLockImage.enabled = abilityProperties2.Cooldown > 0;
        abilityLockText.enabled = abilityProperties2.Cooldown > 0;
        abilityLockText.text = abilityProperties2.Cooldown.ToString();
        abilityButtonImage.enabled = !abilityLockImage.enabled;


        abilityButtonImage = AbilityButton3.GetComponentsInChildren<Image>()[2];
        abilityLockImage = AbilityButton3.GetComponentsInChildren<Image>()[1];
        abilityLockText = abilityLockImage.GetComponentsInChildren<TextMeshProUGUI>()[0];
        abilityButtonImage.sprite = _currentCharacter.Ability3Icon;
        abilityLockImage.enabled = abilityProperties3.Cooldown > 0;
        abilityLockText.enabled = abilityProperties3.Cooldown > 0;
        abilityLockText.text = abilityProperties3.Cooldown.ToString();
        abilityButtonImage.enabled = !abilityLockImage.enabled;


        abilityButtonImage = AbilityButton4.GetComponentsInChildren<Image>()[2];
        abilityLockImage = AbilityButton4.GetComponentsInChildren<Image>()[1];
        abilityLockText = abilityLockImage.GetComponentsInChildren<TextMeshProUGUI>()[0];
        abilityButtonImage.sprite = _currentCharacter.Ability4Icon;
        abilityLockImage.enabled = abilityProperties4.Cooldown > 0;
        abilityLockText.enabled = abilityProperties4.Cooldown > 0;
        abilityLockText.text = abilityProperties4.Cooldown.ToString();
        abilityButtonImage.enabled = !abilityLockImage.enabled;
    }

    public void ActivateAbility1()
    {
        if(_currentCharacter.Actions >= abilityProperties1.ActionCost && abilityProperties1.Cooldown == 0 && !isAiTurn && !isPaused)
        {
            if(_activatedAbilityProperties == null || _activatedAbilityProperties.SkillNumber != 1)
            {
                var abilityProperties = _currentCharacter.Skill1Properties;
                unhighlightButtons();        
                AbilityButton1.GetComponent<Image>().color = new Color32(255, 248, 171, 255);  
                ActivateAbility(abilityProperties);
            }
            else
            {
                _activatedAbilityProperties = null;
                unhighlightButtons(); 
                getMoveRange();
            }
        }
    }

    public void ActivateAbility2()
    {
        if(_currentCharacter.Actions >= abilityProperties2.ActionCost && abilityProperties2.Cooldown == 0 && !isAiTurn && !isPaused)
        {
            if(_activatedAbilityProperties == null || _activatedAbilityProperties.SkillNumber != 2)
            {
                var abilityProperties = _currentCharacter.Skill2Properties;
                unhighlightButtons();   
                AbilityButton2.GetComponent<Image>().color = new Color32(255, 248, 171, 255);  
                ActivateAbility(abilityProperties);
            }
            else
            {
                _activatedAbilityProperties = null;
                unhighlightButtons();   
                getMoveRange();
            }
        }
    }

    public void ActivateAbility3()
    {
        if(_currentCharacter.Actions >= abilityProperties3.ActionCost && abilityProperties3.Cooldown == 0 && !isAiTurn && !isPaused)
        {
            if(_activatedAbilityProperties == null || _activatedAbilityProperties.SkillNumber != 3)
            {
                var abilityProperties = _currentCharacter.Skill3Properties;
                unhighlightButtons();           
                AbilityButton3.GetComponent<Image>().color = new Color32(255, 248, 171, 255);  
                ActivateAbility(abilityProperties);
            }
            else
            {
                _activatedAbilityProperties = null;
                unhighlightButtons();    
                getMoveRange();
            }
        }
    }

    public void ActivateAbility4()
    {
        if(_currentCharacter.Actions >= abilityProperties4.ActionCost && !isAiTurn && !isPaused)
        {
            _selectedTargets.Clear();
            if(_activatedAbilityProperties == null || _activatedAbilityProperties.SkillNumber != 4)
            {
                var abilityProperties = _currentCharacter.Skill4Properties; 
                unhighlightButtons();          
                AbilityButton4.GetComponent<Image>().color = new Color32(255, 248, 171, 255);  
                ActivateAbility(abilityProperties);
            }
            else
            {
                _activatedAbilityProperties = null;
                unhighlightButtons();   
                getMoveRange();
            }
        }
    }

    private void unhighlightButtons()
    {
        AbilityButton1.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        AbilityButton2.GetComponent<Image>().color = new Color32(255, 255, 255, 255);  
        AbilityButton3.GetComponent<Image>().color = new Color32(255, 255, 255, 255);  
        AbilityButton4.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
    }

    public void DrinkPotion()
    {
        if(_currentCharacter.potionCharges > 0 && _currentCharacter.HP !=_currentCharacter.MaxHP && !isAiTurn && !isPaused)
        {
            _currentCharacter.potionCharges -= 1;
            int healedHp = UnityEngine.Random.Range(1, 10);
            _currentCharacter.HP += healedHp;
            if(_currentCharacter.HP > _currentCharacter.MaxHP)
            {
                _currentCharacter.HP = _currentCharacter.MaxHP;
            }
            updateUI();
            _currentCharacter.GetComponent<AudioSource>().PlayOneShot(PotionDrink);
        }
    }

    IEnumerator FadeOut(int index)
    {
        ScoreboardController.Score = 0;
        FadingAnimator.SetBool("Fading", true);
        yield return new WaitUntil(() => FadeImage.color.a == 1);
        UnityEngine.SceneManagement.SceneManager.LoadScene(index);
    }
}
