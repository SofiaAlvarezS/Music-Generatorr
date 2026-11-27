using System.Collections.Generic;
using UnityEngine;

public class MusicalCoordinator : MonoBehaviour
{
    [Header("Coordinacion musical")]

    public bool enableCoordination = true;

    [Range(0f, 1f)]
    public float bassKickConnection = 0.8f;

    [Range(0f, 1f)]
    public float guitarIndependence = 0.7f;

    [Range(0f, 1f)]
    public float dynamicBalance = 0.5f;

    [Header("Tolerancia temporal")]

    [Range(0.01f, 0.25f)]
    public float attackTolerance = 0.08f;

    [Header("Depuracion")]

    public bool printDebug = true;

    private readonly List<float> kickPositions =
        new List<float>();

    private readonly List<float> snarePositions =
        new List<float>();

    private readonly List<float> bassPositions =
        new List<float>();

    private readonly List<float> guitarPositions =
        new List<float>();

    // ==========================================
    // REINICIAR COORDINACION
    // ==========================================

    public void ResetCoordination()
    {
        kickPositions.Clear();
        snarePositions.Clear();
        bassPositions.Clear();
        guitarPositions.Clear();
    }

    // ==========================================
    // REGISTRAR EVENTOS DE BATERIA
    // ==========================================

    public void RegisterDrumHit(
        int midi,
        float position)
    {
        if (midi == 36)
        {
            kickPositions.Add(position);
        }
        else if (midi == 38)
        {
            snarePositions.Add(position);
        }
    }

    // ==========================================
    // REGISTRAR EVENTOS DE BAJO
    // ==========================================

    public void RegisterBassNote(float position)
    {
        bassPositions.Add(position);
    }

    // ==========================================
    // REGISTRAR EVENTOS DE GUITARRA
    // ==========================================

    public void RegisterGuitarNote(float position)
    {
        guitarPositions.Add(position);
    }

    // ==========================================
    // COORDINAR BAJO CON BOMBO
    // ==========================================

    public float AdjustBassPosition(
        float position,
        float measureStart,
        float measureEnd,
        float pulse)
    {
        if (!enableCoordination ||
            kickPositions.Count == 0 ||
            bassKickConnection <= 0f)
        {
            return position;
        }

        float maximumDistance =
            pulse * 0.25f * bassKickConnection;

        float nearestKick = position;
        float bestDistance = maximumDistance;

        foreach (float kick in kickPositions)
        {
            if (kick < measureStart ||
                kick >= measureEnd)
            {
                continue;
            }

            float distance =
                Mathf.Abs(kick - position);

            if (distance <= bestDistance)
            {
                nearestKick = kick;
                bestDistance = distance;
            }
        }

        // Evitar desplazar una nota sobre otra
        // nota de bajo previamente registrada.

        if (HasNearbyAttack(
            bassPositions,
            nearestKick,
            0.025f))
        {
            return position;
        }

        return nearestKick;
    }

    // ==========================================
    // ACENTOS DEL BAJO
    // ==========================================

    public float AdjustBassVelocity(
        float position,
        float velocity)
    {
        if (!enableCoordination)
            return velocity;

        if (HasNearbyAttack(
            kickPositions,
            position,
            attackTolerance))
        {
            velocity *=
                1f + bassKickConnection * 0.15f;
        }

        return Mathf.Clamp01(velocity);
    }

    // ==========================================
    // COORDINAR GUITARRA
    // ==========================================

    public float AdjustGuitarPosition(
        float position,
        float measureStart,
        float measureEnd,
        float pulse)
    {
        if (!enableCoordination ||
            guitarIndependence <= 0f)
        {
            return position;
        }

        bool bassAttack = HasNearbyAttack(
            bassPositions,
            position,
            attackTolerance
        );

        bool kickAttack = HasNearbyAttack(
            kickPositions,
            position,
            attackTolerance
        );

        // Desplazar solamente cuando coinciden
        // el bajo y el bombo.

        if (!bassAttack || !kickAttack)
            return position;

        float alternative =
            position + pulse * 0.25f;

        if (alternative < measureStart ||
            alternative >= measureEnd)
        {
            return position;
        }

        if (HasNearbyAttack(
            bassPositions,
            alternative,
            attackTolerance))
        {
            return position;
        }

        if (HasNearbyAttack(
            kickPositions,
            alternative,
            attackTolerance))
        {
            return position;
        }

        if (HasNearbyAttack(
            guitarPositions,
            alternative,
            attackTolerance))
        {
            return position;
        }

        // Decidir de manera reproducible
        // si se realiza el desplazamiento.

        float decision = Mathf.Abs(
            Mathf.Sin(position * 17.31f)
        );

        if (decision <= guitarIndependence)
            return alternative;

        return position;
    }

    // ==========================================
    // EQUILIBRIO DINAMICO DE GUITARRA
    // ==========================================

    public float AdjustGuitarVelocity(
        float position,
        float velocity)
    {
        if (!enableCoordination)
            return velocity;

        int simultaneousAttacks = 0;

        if (HasNearbyAttack(
            bassPositions,
            position,
            attackTolerance))
        {
            simultaneousAttacks++;
        }

        if (HasNearbyAttack(
            kickPositions,
            position,
            attackTolerance))
        {
            simultaneousAttacks++;
        }

        if (HasNearbyAttack(
            snarePositions,
            position,
            attackTolerance))
        {
            simultaneousAttacks++;
        }

        float reduction =
            simultaneousAttacks *
            dynamicBalance *
            0.12f;

        return Mathf.Clamp01(
            velocity * (1f - reduction)
        );
    }

    // ==========================================
    // DETECTAR ATAQUES CERCANOS
    // ==========================================

    private bool HasNearbyAttack(
        List<float> positions,
        float position,
        float tolerance)
    {
        foreach (float attack in positions)
        {
            if (Mathf.Abs(attack - position)
                <= tolerance)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // INFORMACION DE COORDINACION
    // ==========================================

    public void PrintCoordination()
    {
        if (!printDebug)
            return;

        Debug.Log(
            "COORDINACION MUSICAL" +
            "\nBombo: " + kickPositions.Count +
            "\nCaja: " + snarePositions.Count +
            "\nBajo: " + bassPositions.Count +
            "\nGuitarra: " + guitarPositions.Count
        );
    }
}