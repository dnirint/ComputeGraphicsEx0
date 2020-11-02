using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        Vector3 v = p2 - p1;

        // translation part
        Vector3 offset = (p1 + p2) / 2;
        Matrix4x4 translateMatrix = MatrixUtils.Translate(offset);
        // rotation part
        Matrix4x4 rotateMatrix = RotateTowardsVector(v);
        // scaling part
        var distance = Vector3.Distance(p1, p2) / 2;
        Vector3 scaleVector = new Vector3(diameter, distance, diameter);
        Matrix4x4 scaleMatrix = Matrix4x4.Scale(scaleVector);
        // application part
        Matrix4x4 finalMatrix = translateMatrix * rotateMatrix * scaleMatrix;
        MatrixUtils.ApplyTransform(cylinderObject, finalMatrix);

        return cylinderObject;
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

        Matrix4x4 offsetToParent = MatrixUtils.Translate(joint.offset + parentPosition);
        MatrixUtils.ApplyTransform(joint.gameObject, offsetToParent);

        var jointPosition = joint.gameObject.transform.position;
        foreach (var child in joint.children)
        {
            if (child.isEndSite)
            {
                continue;
            }
            var childGameObject = CreateJoint(child, jointPosition);
            childGameObject.transform.parent = joint.gameObject.transform;
            GameObject cylinder = CreateCylinderBetweenPoints(jointPosition, childGameObject.transform.position, 0.5f);
            cylinder.transform.parent = joint.gameObject.transform;
        }

        return joint.gameObject;
    }

    

    // Transforms BVHJoint according to the keyframe channel data, and recursively transforms its children
    private void TransformJoint(BVHJoint joint, Matrix4x4 parentTransform, float[] keyframe)
    {

        int x = joint.positionChannels[0], y = joint.positionChannels[1], z = joint.positionChannels[2];
        Vector3 positionValues = new Vector3(keyframe[x], keyframe[y], keyframe[z]);

        int rx = joint.rotationChannels[0], ry = joint.rotationChannels[1], rz = joint.rotationChannels[2];
        Vector3 rotationValues = new Vector3(keyframe[rx], keyframe[ry], keyframe[rz]);

        Matrix4x4 translateMatrix = MatrixUtils.Translate(positionValues);

        Matrix4x4 rotationMatrix = Matrix4x4.identity;
        for(int i=0; i<3; i++)
        {
            switch (joint.rotationOrder[i])
            {
                case 0:
                    rotationMatrix *= MatrixUtils.RotateX(rotationValues[0]);
                    //rotationMatrix = MatrixUtils.RotateX(rotationValues[0]) * rotationMatrix;
                    break;
                case 1:
                    rotationMatrix *= MatrixUtils.RotateY(rotationValues[1]);
                    //rotationMatrix = MatrixUtils.RotateY(rotationValues[1]) * rotationMatrix;
                    break;
                case 2:
                    rotationMatrix *= MatrixUtils.RotateZ(rotationValues[2]);
                    //rotationMatrix = MatrixUtils.RotateZ(rotationValues[2]) * rotationMatrix;
                    break;
            }
        }
        

        //Matrix4x4 transformMatrix = parentTransform * translateMatrix * rotationMatrix;
        Matrix4x4 transformMatrix =  translateMatrix * parentTransform * rotationMatrix;
        //Matrix4x4 transformMatrix = parentTransform * translateMatrix * rotationMatrix;

        MatrixUtils.ApplyTransform(joint.gameObject, transformMatrix);

        foreach(var child in joint.children)
        {
            if (child.isEndSite)
            {
                continue;
            }
            TransformJoint(child, transformMatrix, keyframe);
        }



    }

    private float nextActionTime = 0.0f;
    // Update is called once per frame
    void Update()
    {
        if (animate)
        {
            if (Time.time > nextActionTime)
            {
                TransformJoint(data.rootJoint, Matrix4x4.identity, data.keyframes[currFrame]);
                
                currFrame = (currFrame + 1) % data.numFrames;
                nextActionTime += data.frameLength;
                
            }
            
        }
    }
}
