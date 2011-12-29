module DataStructures
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open System
open System.IO
//open System.Collections.Generic

type PointCloud(h, rS, lS, rH, lH, cH) =

    let mutable head = h
    let mutable rightShoulder = rS
    let mutable leftShoulder = lS
    let mutable rightHip = rH
    let mutable leftHip = lH

    let mutable centerHip = cH

    let mutable built = false

    let mutable points = new System.Collections.Generic.List<Vector3>()

    new() = PointCloud(new Vector3(0.0f, 0.0f, 0.0f),new Vector3(0.0f, 0.0f, 0.0f),new Vector3(0.0f, 0.0f, 0.0f),new Vector3(0.0f, 0.0f, 0.0f),new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f))

    //*******************************************************************
    //This member creates a Wavefront OBJ file containing the point cloud data.
    //
    //Parameters: filename
    //*******************************************************************
    member this.ConvertToOBJ filename=
        let fn:string = filename+".obj"
        let strm = new StreamWriter( fn,  false)
       
        for i in points do
            strm.Write ("v "+ i.X.ToString() + " " + i.Y.ToString() + " " + i.Z.ToString() + "\r\n")
        strm.Close()

    member this.LoadOBJ filename=
        let fn:string = filename+".obj"
        let strm = new StreamReader(fn)
        points.Clear() //empties the points list
        while not(strm.EndOfStream) do
            let mutable char = strm.Read() //reads the v at the start of the line
            char <- strm.Read()
            let mutable numberStringX = ""
            while char <> 32 do //not a space
                numberStringX <- numberStringX + char.ToString()
            let mutable numberStringY = ""
            char <- strm.Read()
            while char <> 32 do //not a space
                numberStringY <- numberStringY + char.ToString()
            let mutable numberStringZ = ""
            char <- strm.Read()
            while char <> 32 do //not a space
                numberStringZ <- numberStringZ + char.ToString()
            this.AddPoint(new Vector3(float32(numberStringX), float32(numberStringY), float32(numberStringZ)))
        ()

    //*******************************************************************
    //This member adds a point to the point cloud
    //
    //Parameters: Vector3 point to be added
    //*******************************************************************
    member this.AddPoint point=
        //points <- points @ [point] //concatenates the existing list with the new point. //SLOW
        points.Add(point)

    //*******************************************************************
    //This member adds a series of points to the point cloud
    //
    //Parameters: Collections.Generic.List<Vector3> points to be added
    //*******************************************************************
    member this.AddPoints pointsList=
        points <- pointsList

    //*******************************************************************
    //This member checks if the point cloud is empty
    //
    //*******************************************************************
    member this.pointsEmpty=
        if points.Count = 0 then 
            true
        else 
            false
    //*******************************************************************
    //Accessors for the built flag. 
    //It indicates if the point cloud has been filled. It should be set set by the Depth frame once all points have been added
    //*******************************************************************   
    member this.Built
        with get()=
            built
        and set(setTo)=
            built <- setTo

    member this.Head
        with get() = head
    member this.Hip
        with get() = centerHip

    //*******************************************************************
    //This member clones the data, making a deep copy of the data held.
    //
    //Parameters: Vector3 point to be added
    //*******************************************************************
    member this.Clone =
        let theClone = new PointCloud(head, rightShoulder, leftShoulder, rightHip, leftHip, centerHip)
        theClone.AddPoints points
        theClone

    //*******************************************************************
    //This member transforms the point cloud
    //
    //Parameters: Matrix to transform the points by
    //*******************************************************************
    member this.Transform (transformMatrix:Matrix)=
        let listCopy = new System.Collections.Generic.List<Vector3>()
        for i in points do
            listCopy.Add(Vector3.Transform(i, transformMatrix))
        points <- listCopy