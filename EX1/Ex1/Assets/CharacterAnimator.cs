using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CharacterAnimator : MonoBehaviour
{
    public TextAsset BVHFile; // The BVH file that defines the animation and skeleton
    public bool animate; // Indicates whether or not the animation should be running

    private BVHData data; // BVH data of the BVHFile will be loaded here
    private int currFrame = 0; // Current frame of the animation

    // Start is called before the first frame update
    void Start()
    {
        BVHParser parser = new BVHParser();
        data = parser.Parse(BVHFile);
        CreateJoint(data.rootJoint, Vector3.zero);


    }

    // Returns a Matrix4x4 representing a rotation aligning the up direction of an object with the given v
    Matrix4x4 RotateTowardsVector(Vector3 v)
    {
        Vector3 worldUpDirection = new Vector3(0, 1, 0);


        //preparation
       Vector3 normalizedV = v.normalized;

        var angleX = -Mathf.Atan2(normalizedV[2], normalizedV[1]) * Mathf.Rad2Deg;
        Matrix4x4 rotateX = MatrixUtils.RotateX(angleX);

        var a = normalizedV[0];
        var b = normalizedV[1];
        var c = normalizedV[2];
        var sqrtResult = Mathf.Sqrt(Mathf.Pow(c, 2) + Mathf.Pow(b, 2));
        var angleZ = Mathf.Atan2(a, sqrtResult) * Mathf.Rad2Deg;
        Matrix4x4 rotateZ = MatrixUtils.RotateZ(angleZ);

        var generalRotate = rotateZ * rotateX;
        var inverseGeneralRotate = generalRotate.inverse;

        // todo DELETE THIS BEFORE SUBMISSION
        Vector3 testRotateResult = inverseGeneralRotate.MultiplyVector(worldUpDirection);
        bool verification = testRotateResult == v.normalized;
        Debug.Assert(verification);

        return inverseGeneralRotate;
    }

    // Creates a Cylinder GameObject between two given points in 3D space
    GameObject CreateCylinderBetweenPoints(Vector3 p1, Vector3 p2, float diameter)
    {
        GameObject cylinderObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        return null;
    }

    private static Vector3 scaleBy2Vector = new Vector3(2, 2, 2);
    private static Matrix4x4 scaleBy2Matrix = Matrix4x4.Scale(scaleBy2Vector);
    private static Vector3 scaleBy8Vector = new Vector3(8, 8, 8);
    private static Matrix4x4 scaleBy8Matrix = Matrix4x4.Scale(scaleBy8Vector);

    // Creates a GameObject representing a given BVHJoint and recursively creates GameObjects for it's child joints
    GameObject CreateJoint(BVHJoint joint, Vector3 parentPosition)
    {
        joint.gameObject = new GameObject(joint.name);
        
        GameObject sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereObject.transform.parent = joint.gameObject.transform;

        Matrix4x4 scaleMatrix = (joint.name.Equals("Head")) ? scaleBy8Matrix : scaleBy2Matrix;
        MatrixUtils.ApplyTransform(sphereObject, scaleMatrix);

        Matrix4x4 offsetToParent = MatrixUtils.Translate(joint.offset+parentPosition);
        MatrixUtils.ApplyTransform(joint.gameObject, offsetToParent);

        foreach (var child in joint.children)
        {
            CreateJoint(child, joint.gameObject.transform.position);
        }

        return joint.gameObject;
    }

    // Transforms BVHJoint according to the keyframe channel data, and recursively transforms its children
    private void TransformJoint(BVHJoint joint, Matrix4x4 parentTransform, float[] keyframe)
    {
        // Your code here
    }

    // Update is called once per frame
    void Update()
    {
        if (animate)
        {
            // Your code here
        }
    }
}
