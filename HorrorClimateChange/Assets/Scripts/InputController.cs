using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{

    [HideInInspector] public float m_vertical;
    [HideInInspector] public float m_horizontal;
    [HideInInspector] public bool m_shootDown;
    [HideInInspector] public bool m_shootUp;

    void Update()
    {
        m_horizontal = Input.GetAxis("Horizontal");
        m_vertical = Input.GetAxis("Vertical");
        m_shootDown = Input.GetButtonDown("Fire1");
        m_shootUp = Input.GetButtonUp("Fire1");

    }

    public bool IsWalking ()
    {
        if ((int)m_horizontal != 0 || (int)m_vertical != 0)
        {
            return true;
        }
            return false;
    }
}
