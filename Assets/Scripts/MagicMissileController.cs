using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MagicMissileController : MonoBehaviour
{
   public GameObject target;
   public List<Vector3Int> targetTiles;
   public Tilemap tilemap;
   public GameObject MissMessage;
   private Vector3Int _currentTile;
   public BaseCharacterController Caster;
   public int damage;

   void Start()
   {
      StartCoroutine(destroyAfterTime());
   }
 

   private void Update()
   {
      var currentWorldPosition = Camera.main.ScreenToWorldPoint(transform.position);
      var currentTile = tilemap.WorldToCell(transform.position);
      if(currentTile != _currentTile)
      {
         if(targetTiles.Contains(_currentTile))
         {
            Instantiate(MissMessage, transform.position, Quaternion.identity);
         }
         _currentTile = currentTile;
      }
      
      if(targetTiles.Contains(_currentTile))
      {
         target.GetComponent<EnemyController>().GetHit(Caster, damage);
         Destroy(gameObject);
      }
   }

   IEnumerator destroyAfterTime()
   {
      yield return new WaitForSeconds(2f);
      Destroy(gameObject);
   }
}
