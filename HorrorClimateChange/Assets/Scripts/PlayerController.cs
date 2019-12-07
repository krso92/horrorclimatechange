using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(InputController))]
public class PlayerController : MonoBehaviour
{

    Rigidbody rigidbody;
    public float speed;
    InputController inputController;
    public Flashlight flashlight;

    public int health;
    Vector3 lookPos;
    //InputController m_input;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        inputController = GetComponent<InputController>();
        SceneManager.sceneLoaded += PlayerReset;
    }

    // Update is called once per frame
    private void Update()
    {
        RotatePlayerAlongMousePosition();
        MovePlayer();
    }

    private void MovePlayer()
    {
        Vector3 movement = new Vector3(inputController.m_horizontal, 0, inputController.m_vertical);
        //rigidbody.AddForce(movement.normalized * speed );
        transform.Translate(movement.normalized * speed * Time.deltaTime,Space.World);

        if (inputController.m_shootDown)
        {
            flashlight.BurnLight();
        }
        if (inputController.m_shootUp)
        {
            flashlight.NormalLight();
        }

    }

    void FixedUpdate()
    {
    }


    void LookAt()
    {

    }

    void RotatePlayerAlongMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100))
        {
            lookPos = hit.point;
        }

        Vector3 lookDir = lookPos - transform.position;
        lookDir.y = 0;

        transform.LookAt(transform.position + lookDir, Vector3.up);
    }

    public void DamagePlayer(int dmg)
    {
        health = health - dmg;
        if (health <= 0)
        {
            PlayerDeath();
        }
    }

    void PlayerDeath()
    {

    }

    void PlayerReset(Scene scene, LoadSceneMode mode)
    {
        Vector3 pos = GameObject.Find("PlayerSpawnPlace").transform.position;
        transform.position = pos;
        pos.y = 10f;
        Camera.main.transform.position = pos;
        Debug.Log("Player reset");
    }
}
