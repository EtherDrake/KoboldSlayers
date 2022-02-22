using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SwordProjectile : MonoBehaviour
{
   // Start is called before the first frame update
   public BaseCharacterController target;
   public int damage;

   void Start()
   {
      StartCoroutine(destroyAfterTime());
   }
 

   private void Update()
   {
      if(Mathf.Abs(transform.position.x - target.transform.position.x) < 0.25f && Mathf.Abs(transform.position.y - target.transform.position.y) < 0.25f)
      {
        target.GetComponent<BaseCharacterController>().GetHit(null, damage);
        Destroy(gameObject);
      }
   }

   IEnumerator destroyAfterTime()
   {
      yield return new WaitForSeconds(2f);
      Destroy(gameObject);
   }
}
