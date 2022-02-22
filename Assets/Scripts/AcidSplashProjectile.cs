using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class AcidSplashProjectile : MonoBehaviour
{
    public Dictionary<GameObject, int> targets;
    public List<GameObject> dodged;
    public Vector3Int targetTile;
    public Tilemap tilemap;
    public GameObject MissMessage;
    private Vector3Int _currentTile;
    private bool dodgeMessageShown;
    private bool damageMessageShown;
    public BaseCharacterController Caster;

    // Start is called before the first frame update
    void Start()
    {        
      StartCoroutine(destroyAfterTime());
    }

    // Update is called once per frame
   private void Update()
   {
      var currentWorldPosition = Camera.main.ScreenToWorldPoint(transform.position);
      var currentTile = tilemap.WorldToCell(transform.position);
      if(currentTile != _currentTile)
      {
        _currentTile = currentTile;
      }
      
      if(_currentTile == targetTile)
      {
        if(!damageMessageShown)
        {
          foreach(var target in targets)
          {
            target.Key.GetComponent<EnemyController>().GetHit(Caster, target.Value);
          }
          damageMessageShown = true;
        }
        if(!dodgeMessageShown)
        {
          foreach(var target in dodged)
          {
            var missMessagePosition = new Vector3(target.transform.position.x + 0.275f, target.transform.position.y + 0.25f, target.transform.position.z);
            Instantiate(MissMessage, missMessagePosition, Quaternion.identity);
          }
          dodgeMessageShown = true;
        }

        GetComponent<Rigidbody2D>().velocity = Vector3.forward * 0;
        transform.rotation = Quaternion.identity; 
        transform.position = tilemap.CellToWorld(targetTile) + new Vector3(0.5f, 0.25f, 0);
        transform.localScale = new Vector3(3, 3, 1);

        GetComponent<Animator>().SetTrigger("Explode");
        Destroy(gameObject, 0.5f);
      }
   }

    

   IEnumerator destroyAfterTime()
   {
      yield return new WaitForSeconds(2f);
      Destroy(gameObject);
   }
}
