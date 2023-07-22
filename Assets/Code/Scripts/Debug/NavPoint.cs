using UnityEngine;

namespace Scripts.Diagnostics
{
    public class NavPoint : MonoBehaviour
    {
        void Start()
        {
            name = this.ToString();
        }

        public override string ToString()
        {
            return $"NavPoint ({transform.position.y:0.00}, {transform.position.x:0.00})";
        }
    }
}