using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;


public class CharacterAnimator : MonoBehaviour
{
    public TextAsset BVHFile; // The BVH file that defines the animation and skeleton
    public bool animate; // Indicates whether or not the animation should be running

    private BVHData data; // BVH data of the BVHFile will be loaded here
    private int currFrame = 0; // Current frame of the animation
    private static readonly Vector3 scaleBy2Vector = new Vector3(2, 2, 2);
    private static readonly Matrix4x4 scaleBy2Matrix = Matrix4x4.Scale(scaleBy2Vector);
    private static readonly Vector3 scaleBy8Vector = new Vector3(8, 8, 8);
    private static readonly Matrix4x4 scaleBy8Matrix = Matrix4x4.Scale(scaleBy8Vector);

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
        Vector3 normalizedV = v.normalized;
        // rotate about the X axis
        var angleX = -Mathf.Atan2(normalizedV[2], normalizedV[1]) * Mathf.Rad2Deg;
        Matrix4x4 rotateX = MatrixUtils.RotateX(angleX);
        // rotate about the z axis
        float a = normalizedV[0], b = normalizedV[1], c = normalizedV[2];
        var sqrtResult = Mathf.Sqrt(Mathf.Pow(c, 2) + Mathf.Pow(b, 2));
        var angleZ = Mathf.Atan2(a, sqrtResult) * Mathf.Rad2Deg;
        Matrix4x4 rotateZ = MatrixUtils.RotateZ(angleZ);
        // apply the rotations and generate the inverse to them
        var generalRotate = rotateZ * rotateX;
        var inverseGeneralRotate = generalRotate.inverse;

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



    // Creates a GameObject representing a given BVHJoint and recursively creates GameObjects for it's child joints
    GameObject CreateJoint(BVHJoint joint, Vector3 parentPosition)
    {
        // create joint game object and define it as parent of the new sphere object
        joint.gameObject = new GameObject(joint.name);
        GameObject sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereObject.transform.parent = joint.gameObject.transform;
        // scale the sphere according to joint type
        Matrix4x4 scaleMatrix = (joint.name.Equals("Head")) ? scaleBy8Matrix : scaleBy2Matrix;
        MatrixUtils.ApplyTransform(sphereObject, scaleMatrix);
        // offset the joint game object in relation to it's parent's position
        Matrix4x4 offsetToParent = MatrixUtils.Translate(joint.offset + parentPosition);
        MatrixUtils.ApplyTransform(joint.gameObject, offsetToParent);
        // Generate cylinder objects between the Joint and it's children (ignore EndSites)
        var jointPosition = joint.gameObject.transform.position;
        foreach (var child in joint.children)
        {
            var childGameObject = CreateJoint(child, jointPosition);

            GameObject cylinder = CreateCylinderBetweenPoints(jointPosition, childGameObject.transform.position, 0.5f);
            cylinder.transform.parent = joint.gameObject.transform;

        }

        return joint.gameObject;
    }

    

    // Transforms BVHJoint according to the keyframe channel data, and recursively transforms its children
    private void TransformJoint(BVHJoint joint, Matrix4x4 parentTransform, float[] keyframe)
    {
        // if no position is given we only offset the joint based on it's offset value
        Matrix4x4 translateMatrix = MatrixUtils.Translate(joint.offset);
        if (joint.positionChannels != Vector3.zero)
        {
            int px = joint.positionChannels[0], py = joint.positionChannels[1], pz = joint.positionChannels[2];
            Vector3 positionValues = new Vector3(keyframe[px], keyframe[py], keyframe[pz]);
            translateMatrix = MatrixUtils.Translate(positionValues);
        }

        // same here, if no rotation is given, we do not rotate the joint (this is why we use an identity matrix)
        Matrix4x4 rotationMatrix = Matrix4x4.identity;
        if (joint.rotationChannels != Vector3.zero)
        {
            int rx = joint.rotationChannels[0], ry = joint.rotationChannels[1], rz = joint.rotationChannels[2];
            Vector3 rotationValues = new Vector3(keyframe[rx], keyframe[ry], keyframe[rz]);
            // apply rotations in the correct order
            for (int i = 0; i < 3; i++)
            {
                switch (joint.rotationOrder[i])
                {
                    case 0:
                        rotationMatrix *= MatrixUtils.RotateX(rotationValues[0]);
                        break;
                    case 1:
                        rotationMatrix *= MatrixUtils.RotateY(rotationValues[1]);
                        break;
                    case 2:
                        rotationMatrix *= MatrixUtils.RotateZ(rotationValues[2]);
                        break;
                }
            }
        }

        Matrix4x4 transformMatrix = parentTransform * translateMatrix * rotationMatrix;

        MatrixUtils.ApplyTransform(joint.gameObject, transformMatrix);

        foreach(var child in joint.children)
        {
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
                // in each relevant frame, we compute the next time we need to update the frame
                // we also compute the current frame based on how many frames we missed since the expected time
                currFrame = (int)Mathf.Floor(Time.time / data.frameLength) % data.numFrames;
                TransformJoint(data.rootJoint, Matrix4x4.identity, data.keyframes[currFrame]);               
                nextActionTime = Time.time + data.frameLength;
            }
 
        }
    }
}
