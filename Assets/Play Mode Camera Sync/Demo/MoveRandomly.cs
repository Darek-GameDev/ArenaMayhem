using UnityEngine;

namespace PlayModeCameraSync
{
    public class MoveRandomly : MonoBehaviour
    {
        [Header("Movement Bounds")]
        [Tooltip("Minimum X position")]
        public float minX = -10f;

        [Tooltip("Maximum X position")]
        public float maxX = 10f;

        [Tooltip("Minimum Z position")]
        public float minZ = -10f;

        [Tooltip("Maximum Z position")]
        public float maxZ = 10f;

        [Header("Movement Settings")]
        [Tooltip("Speed of movement in units per second")]
        public float moveSpeed = 5f;

        [Tooltip("Distance threshold to consider target reached")]
        public float arrivalDistance = 0.1f;

        [Tooltip("Speed of rotation in degrees per second")]
        public float rotationSpeed = 360f;

        private Vector3 targetPosition;
        private bool hasTarget = false;

        private void Start()
        {
            ChooseNewTarget();
        }

        private void Update()
        {
            if (!hasTarget)
            {
                ChooseNewTarget();
            }

            MoveTowardsTarget();
        }

        private void ChooseNewTarget()
        {
            float randomX = Random.Range(minX, maxX);
            float randomZ = Random.Range(minZ, maxZ);

            targetPosition = new Vector3(randomX, transform.position.y, randomZ);
            hasTarget = true;
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPosition) < arrivalDistance)
            {
                hasTarget = false;
            }
        }
    }
}