using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    [SerializeField] private Transform robot_target;
    [SerializeField] private Transform player;
    [SerializeField] private float y_position;

    private void Update()
    {
        transform.LookAt(player);
        Vector3 target_position = new Vector3(robot_target.position.x, robot_target.position.y, robot_target.position.z);
        Vector3 new_location = Vector3.Lerp(transform.position, target_position, Time.deltaTime);
        transform.position = new_location;
    }
}
