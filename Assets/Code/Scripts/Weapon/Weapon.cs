using UnityEngine;

namespace Scripts.Weapon
{
    public class Weapon : MonoBehaviour
    {
        public Transform firePoint;
        public GameObject bulletPrefab;

        public void Shoot()
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            bulletPrefab.layer = Utils.World.GetLayerID("Default");
        }
    }
}