using UnityEngine;

namespace ChickenPathfinding
{
    public class MoveTarget : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _changeDirectionTime;
        private float _changeDirectionTimer = 0; 
        private int _directionMultiplier = 1;

        public void Update()
        {
            if (_changeDirectionTimer <= 0)
            {
                _changeDirectionTimer = _changeDirectionTime; 
                _directionMultiplier *= -1;
            }

            _changeDirectionTimer -= Time.deltaTime;
            
            transform.Translate(new (_moveSpeed * _directionMultiplier * Time.deltaTime, 0f, 0f));
        }
    }
}