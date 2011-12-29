// Learn more about F# at http://fsharp.net
namespace main
    module game=
        open Microsoft.Xna.Framework
        open Microsoft.Xna.Framework.Audio
        open Microsoft.Xna.Framework.Graphics
        open Microsoft.Xna.Framework.Input
        open Microsoft.Xna.Framework.Media

        open Microsoft.Research.Kinect.Nui
        open System.Linq
        open System.IO
        open System

        open DataStructures
        open KinectHelperMethods

        type XnaGame() as this =
            inherit Game()
    
            let screenWidth = 640
            let screenHeight = 480

            do this.Content.RootDirectory <- "XnaGameContent"
            let graphicsDeviceManager = new GraphicsDeviceManager(this)

            let mutable frontMeasure:int[] = null
            let mutable backMeasure:int[] = null
            let mutable sideMeasure = null
            let mutable frontToBack =0.0

            let mutable singlePress = true

            let mutable sprite : Texture2D = null
            let mutable LSsprite : Texture2D = null
            let mutable RSsprite : Texture2D = null
            let mutable depthImg : Texture2D = null
            let mutable dotImg : Texture2D = null
            let mutable whiteHorizontalBar : Texture2D = null

            let mutable spriteBatch : SpriteBatch = null
            let mutable leftHandPos = new Vector3(0.0f,0.0f, 0.0f)
            let mutable rightHandPos = new Vector3(0.0f,0.0f, 0.0f)
            let mutable headPos = new Vector3(0.0f,0.0f, 0.0f)
            let mutable leftFootPos = new Vector3(0.0f,0.0f, 0.0f)
            let mutable rightFootPos = new Vector3(0.0f,0.0f, 0.0f)

            let mutable topOfHead = new Vector3()
            let mutable bottomOfFeet = new Vector3()

            let mutable timeTillGo = 7000

            let mutable clickSound:SoundEffect = null
            let mutable bleepSound:SoundEffect = null

            let mutable measurementFont:SpriteFont =null
            let mutable measurement =0.0

            let mutable shoulderAngle =0.0

            let kinect = new KinectSensor(this)

            let measureFlatDistance (points:int[]) =
                let mutable lastPixel =0
                let mutable measurement = 0.0
                let mutable firstPixel =0
                let mutable i=0
                while firstPixel = 0 && i<points.Length-1 do //find the first non 0 pixel. i.e. the first pixel that is on the player
                    firstPixel <- points.[i]
                    lastPixel <- points.[i]
                    i<-i+1
                firstPixel <- i
                while not(lastPixel=0) && i<points.Length-1 do
                    lastPixel <- points.[i]
                    let pixelWidth = 374.0 / (80096.0 * Math.Pow(float(points.[0]), -0.953)) //get width of the pixel being considered
                    measurement <- measurement + pixelWidth 
                    i<-i+1
                lastPixel <- i
                measurement

            let measureSurfaceDistance (points:int[]) =
                let mutable measurement = 0.0
                let mutable pixelWidth=0.0
                let mutable lastPixelDepth =0
                let mutable i=0
                while lastPixelDepth = 0 && i<points.Length-1 do
                    lastPixelDepth <- points.[i]
                    i<-i+1
                while i < points.Length-1 do
                    pixelWidth <- 374.0 / (80096.0 * Math.Pow(float(points.[i]), -0.953))
                    if points.[i] >0 then
                        let currentPixelDepthChange = Math.Sqrt(Math.Pow(float(points.[i] - lastPixelDepth),2.0))
                        //By pythagoras
                        let diagonalWH = Math.Sqrt(Math.Pow(currentPixelDepthChange,2.0) + Math.Pow(pixelWidth, 2.0))
                        measurement <- measurement + diagonalWH
                        lastPixelDepth <- points.[i]
                    i<-i+1
                measurement

            let flip (points:int[]) =
                let max = points |> Array.max
                let flippedPoints =  points |> Array.map (fun a -> a-max) |> Array.map (fun a -> a * -1)
                let newPoints = flippedPoints |> Array.map (fun a -> 
                                                      let mutable b=0
                                                      if not(a=0) then
                                                         b <-a+max
                                                      b) 
                newPoints

            override game.Initialize() =
                graphicsDeviceManager.GraphicsProfile <- GraphicsProfile.Reach
                graphicsDeviceManager.PreferredBackBufferWidth <- screenWidth
                graphicsDeviceManager.PreferredBackBufferHeight <- screenHeight
                graphicsDeviceManager.ApplyChanges() 
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)

                game.Components.Add(kinect)

                base.Initialize()

            override game.LoadContent() =
                sprite <- game.Content.Load<Texture2D>("Sprite")
                LSsprite <- game.Content.Load<Texture2D>("ls")
                RSsprite <- game.Content.Load<Texture2D>("rs")
                dotImg <- game.Content.Load<Texture2D>("dot")
                whiteHorizontalBar <- game.Content.Load<Texture2D>("whiteHorizontalBar")
                measurementFont <- game.Content.Load<SpriteFont>("font1")
                clickSound <- game.Content.Load<SoundEffect>("click_1")
                bleepSound <- game.Content.Load<SoundEffect>("BEEP2B")

            override game.Update gameTime = 
                timeTillGo <- timeTillGo - gameTime.ElapsedGameTime.Milliseconds
                if (timeTillGo % 1000) = 0 && timeTillGo >1 then
                    clickSound.Play ()
                    ()
                if timeTillGo <0 && timeTillGo > -3000 && frontMeasure = null then
                    //System.Diagnostics.Debug.WriteLine("front")
                    bleepSound.Play ()
                    kinect.grabPoints
                    frontMeasure <- kinect.LineDepths
                    //measurement <- measureSurfaceDistance frontMeasure
                if timeTillGo < -3000 && timeTillGo > -6000 && sideMeasure = null then
                    System.Diagnostics.Debug.WriteLine("side")
                    bleepSound.Play ()
                    kinect.grabPoints
                    sideMeasure <- kinect.LineDepths
                    //frontToBack <- measureFlatDistance sideMeasure
                elif timeTillGo < -6000 && backMeasure = null then
                    //System.Diagnostics.Debug.WriteLine("back=")
                    bleepSound.Play ()
                    kinect.grabPoints
                    backMeasure <- kinect.LineDepths
                    //let adjustment = frontToBack - float(backMeasure.Max() - backMeasure.Min() + (frontMeasure.Max() - frontMeasure.Min()))
                    //measurement <- measurement + (measureSurfaceDistance backMeasure) + (adjustment * 2.0)

                if Keyboard.GetState().IsKeyUp(Keys.A) then singlePress <- true

                base.Update gameTime

            override game.Draw gameTime = 
                game.GraphicsDevice.Clear(Color.CornflowerBlue)
                spriteBatch.Begin()
                if not(depthImg = null) then spriteBatch.Draw(depthImg, new Rectangle(0, 0, screenWidth, screenHeight), Color.White)
                spriteBatch.Draw(LSsprite, new Vector2(leftHandPos.X * 2.0f - 20.0f, leftHandPos.Y * 2.0f - 20.0f), Color.White)
                spriteBatch.Draw(RSsprite, new Vector2(rightHandPos.X * 2.0f - 20.0f, rightHandPos.Y * 2.0f - 20.0f), Color.White)
                //spriteBatch.Draw(sprite, new Vector2(headPos.X - 20.0f, headPos.Y - 20.0f), Color.White)
                spriteBatch.Draw(LSsprite, new Vector2(leftFootPos.X * 2.0f - 20.0f, leftFootPos.Y * 2.0f - 20.0f), Color.White)
                spriteBatch.Draw(RSsprite, new Vector2(rightFootPos.X * 2.0f - 20.0f, rightFootPos.Y * 2.0f - 20.0f), Color.White)

                if not(frontMeasure = null) then
                    for i = 0 to frontMeasure.Length-1 do
                        spriteBatch.Draw(dotImg, new Vector2(float32(i*2), float32(frontMeasure.[i]/3)), (if frontMeasure.[i]>0 then Color.White else Color.Black))
                if not(backMeasure = null) then
                    for i = 0 to backMeasure.Length-1 do
                        spriteBatch.Draw(dotImg, new Vector2(float32(i*2), float32(backMeasure.[i]/3)), Color.DarkOliveGreen)
                spriteBatch.Draw(whiteHorizontalBar, new Vector2(0.0f, float32(topOfHead.Y * 2.0f)), Color.DarkOliveGreen)
                spriteBatch.Draw(whiteHorizontalBar, new Vector2(0.0f, float32(bottomOfFeet.Y * 2.0f)), Color.White)
                    
                spriteBatch.DrawString(measurementFont, measurement.ToString(), new Vector2(0.0f, 440.0f), Color.White);
        
                spriteBatch.End()

            member game.PassJoints leftShoulder rightShoulder head leftHip rightHip=
                leftHandPos <- leftShoulder
                rightHandPos <- rightShoulder
                headPos <- head
                leftFootPos <- leftHip
                rightFootPos <- rightHip
            member game.DrawVerticalBounds top bottom=
                topOfHead <- top
                bottomOfFeet <- bottom

            member game.SetDepthImg img=
                depthImg <- img

            member game.ScreenDimensions
                with get() = (screenWidth, screenHeight)
            
            

        //*********************************************************************************************************
        //*********************************************************************************************************
        //******************** KinectSensor Class *****************************************************************
        //*********************************************************************************************************
        //*********************************************************************************************************
        
        
        and KinectSensor(game:XnaGame)=
                    inherit DrawableGameComponent(game)
            

                    let nui = new Runtime()

                    let mutable distancesOnLine = Array.create 320 0
                    let mutable img:Texture2D = null
                    let mutable skeleton:SkeletonData = null

                    let mutable max = 0

                    let mutable lastPointCloud = new PointCloud()
                    let mutable currentPointCloud = new PointCloud()
                    let mutable frontPointCloud = new PointCloud()
                    let mutable sidePointCloud = new PointCloud()
                    let mutable backPointCloud = new PointCloud()

                    let mutable body = new BodyData.BodyMeasurements()

                    let Scale ( maxPixel:int, maxSkeleton:float, position:float)=
                        let mutable scale = (((((float) maxPixel / maxSkeleton) / 2.0) * position) + (float)(maxPixel/2))
                        if scale > (float)maxPixel then
                            scale <- (float)maxPixel
                        if scale < (float)0 then
                            scale <- (float)0
                        (float32)scale

                    let ScaleTo (joint:Joint, width, height, skeletonMaxX, skeletonMaxY)=
                        new Vector3(Scale(width, skeletonMaxX, (float)joint.Position.X), Scale(height, skeletonMaxY, -(float)joint.Position.Y), Scale(height, skeletonMaxY, -(float)joint.Position.Z))
            

                    //**************
                    //NEW SCALING METHODS
                    //**************

                   
                    let SkeletonReady (sender : obj) (args: SkeletonFrameReadyEventArgs)=
                        let mutable i=0
                        let mutable skeleton = null
                        while skeleton = null && i< args.SkeletonFrame.Skeletons.Length do
                            if args.SkeletonFrame.Skeletons.ElementAt(i).TrackingState = SkeletonTrackingState.Tracked then
                        
                                skeleton <- args.SkeletonFrame.Skeletons.ElementAt(i)
                                let depthWidth, depthHeight = 320, 240
                                let leftShoulderJ = skeleton.Joints.[JointID.ShoulderLeft]
                                let leftShoulder = new Vector3(leftShoulderJ.GetScreenPosition(nui, 320, 240).X, leftShoulderJ.GetScreenPosition(nui, 320, 240).Y, leftShoulderJ.Position.Z )
                                let rightShoulderJ = skeleton.Joints.[JointID.ShoulderRight]
                                let rightShoulder = new Vector3(rightShoulderJ.GetScreenPosition(nui, 320, 240).X, rightShoulderJ.GetScreenPosition(nui, 320, 240).Y, rightShoulderJ.Position.Z )
                                let centerShoulderJ = skeleton.Joints.[JointID.ShoulderCenter]
                                let centerShoulder = new Vector3(centerShoulderJ.GetScreenPosition(nui, 320, 240).X, centerShoulderJ.GetScreenPosition(nui, 320, 240).Y, centerShoulderJ.Position.Z )
                                let headJ = skeleton.Joints.[JointID.Head]
                                let head = new Vector3(headJ.GetScreenPosition(nui, 320, 240).X, headJ.GetScreenPosition(nui, 320, 240).Y, headJ.Position.Z )
                                let leftHipJ = skeleton.Joints.[JointID.HipLeft]
                                let leftHip = new Vector3(leftHipJ.GetScreenPosition(nui, 320, 240).X, leftHipJ.GetScreenPosition(nui, 320, 240).Y, leftHipJ.Position.Z )
                                let rightHipJ = skeleton.Joints.[JointID.HipRight]
                                let rightHip = new Vector3(rightHipJ.GetScreenPosition(nui, 320, 240).X, rightHipJ.GetScreenPosition(nui, 320, 240).Y, rightHipJ.Position.Z )
                                let centerHipJ = skeleton.Joints.[JointID.HipCenter ]
                                let centerHip = new Vector3(centerHipJ.GetScreenPosition(nui, 320, 240).X, centerHipJ.GetScreenPosition(nui, 320, 240).Y, centerHipJ.Position.Z )
                                let leftFootJ = skeleton.Joints.[JointID.FootLeft]
                                let leftFoot = new Vector3(leftFootJ.GetScreenPosition(nui, 320, 240).X, leftFootJ.GetScreenPosition(nui, 320, 240).Y, leftFootJ.Position.Z )
                                let rightFootJ = skeleton.Joints.[JointID.FootRight]
                                let rightFoot = new Vector3(rightFootJ.GetScreenPosition(nui, 320, 240).X, rightFootJ.GetScreenPosition(nui, 320, 240).Y, rightFootJ.Position.Z )

                                currentPointCloud <- new PointCloud(head, rightShoulder, leftShoulder, rightHip, leftHip, centerHip)
                                body.SetSkeleton(head, leftShoulder, rightShoulder, centerShoulder, leftHip, rightHip, centerHip, leftFoot, rightFoot)

                                game.PassJoints leftShoulder rightShoulder head leftHip rightHip
                            i<-i+1
            
                    let mutable measureLine =100  
                    let mutable lastImageFrameReadyArgs:ImageFrameReadyEventArgs=null  
                    let mutable distancesArray = Array.create (320*240) 0

                    let DepthReady (sender : obj) (args:ImageFrameReadyEventArgs)=
                        lastPointCloud <- currentPointCloud.Clone
                        lastImageFrameReadyArgs <- args
                        let maxDist = 4000
                        let minDist = 850

                        let distOffset = maxDist - minDist

                        let pImg = args.ImageFrame.Image
                        img <- new Texture2D(game.GraphicsDevice, pImg.Width, pImg.Height)
                        let DepthColor = Array.create (pImg.Width*pImg.Height) (new Color(255,255,255))
                        let newCloud = currentPointCloud.pointsEmpty
                    
                        for y = 0 to pImg.Height-1 do
                            for x = 0 to pImg.Width-1 do
                                let n = (y * pImg.Width + x) * 2
                                let distance = (int pImg.Bits.[n + 0] >>>3) ||| (int pImg.Bits.[n + 1] <<< 5) //put together bit data as depth
                                let pI = int (pImg.Bits.[n] &&& 7uy) // gets the player index
                        
                                distancesArray.[y*pImg.Width + x] <- if pI > 0 then distance else 0

                                let mutable intensity = 255-(255*Math.Max(int(distance-minDist),0)/distOffset) //convert distance into a gray level value between 0 and 255 taking into account min and max distances of the kinect.
                                let mutable colour = new Color(intensity, intensity, intensity)
                                if y=measureLine && pI = 1 then
                                    distancesOnLine.[x] <- distance
                                    colour <- new Color(255, 0, 0)
                                if pI = 0 then //if not a player
                                    distancesOnLine.[x] <- 0
                                    colour <- new Color(0, 0, 0)
                                DepthColor.[y * pImg.Width + x] <- colour

                                if newCloud && pI > 0 then
                                    currentPointCloud.AddPoint (new Vector3(float32(x*5), float32(y*5), float32(distance)))

                        img.SetData(DepthColor)
                        max <- distancesArray.Max()
                        let copy =  Array.create distancesArray.Length 0
                        Array.Copy(distancesArray, copy, distancesArray.Length)

                        body.SetDepthData copy
                        //System.Diagnostics.Debug.WriteLine( "Max:" + distancesArray.Max().ToString() )
                        //game.DrawHorizontalLine body.GetTopOfHead
                        currentPointCloud.Built <- true
                        game.SetDepthImg img

                    //let mutable pointCloud = "# List of Vertices, with (x,y,z[,w]) coordinates, w is optional. \r\n" //initialize pointcloud string.
                    let mutable Ppressed = false
                    let mutable frontDone = 1

                    
                    let strm2 = new StreamWriter("pointCloud.obj", false) 
                    do strm2.Write("")
                    do strm2.Close()

                    override this.Initialize ()=
                        try 
                            do nui.Initialize(RuntimeOptions.UseSkeletalTracking ||| RuntimeOptions.UseDepthAndPlayerIndex)
                            //do nui.SkeletonEngine.TransformSmooth <- true;
                            do nui.SkeletonFrameReady.AddHandler(new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonReady))
                            do nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex)
                            do nui.DepthFrameReady.AddHandler(new EventHandler<ImageFrameReadyEventArgs>(DepthReady))
                        with
                            | :? System.InvalidOperationException -> System.Diagnostics.Debug.Write("Kinect not connected!")
                            

                    override this.Draw gameTime=
                        base.Draw gameTime

//                    member this.generatePointCloud (depths:int[], back:bool, pointCloud:PointCloud)= 
//                        if not(depths = null) then
//                    
//                            let mutable flipped = Array.create (320*240) 0
//                            for y=0 to 239 do
//                                for x=0 to 319 do
//                                    let n = ((y)* 320 + x)
//                                    flipped.[n] <- depths.[y*320 + (319-x)]
//
//                            System.Diagnostics.Debug.WriteLine("DepthsMax:" + Array.max(depths).ToString())
//                            System.Diagnostics.Debug.WriteLine("FlippedMax:" + Array.max(flipped).ToString())
//                            for y = 0 to 240-1 do
//                                for x = 0 to 320-1 do
//                                    let n = (y * 320 + x)
//                                    if back && (flipped.[n] > 1) then
//                                        let newY = (y*5)
//                                        pointCloud.AddPoint (new Vector3(float32(x*5), float32(newY), float32(flipped.[n])))
//                                
//                                    elif not(back) && depths.[n] > 1 then
//                                        pointCloud.AddPoint (new Vector3(float32(x*5), float32(y), float32(depths.[n])))
//                    

//                    member this.mirror (back:int[][])=
//                        Array.iter(fun a-> Array.Reverse a) back
//                        back
//                
//
//                    member this.outputPCFile =
//                        let strm = new StreamWriter("pointCloud.obj") 
//                        strm.Write(pointCloud)
//                        strm.Close()


                    override this.Update gameTime=
                
                        if body.completeBody then
                                game.DrawVerticalBounds body.GetTopOfHead body.GetBottomOfFeet
                        
                        if Keyboard.GetState().IsKeyDown(Keys.Down) && measureLine < snd(game.ScreenDimensions)/2 then 
                            measureLine <-  measureLine+1
                        if Keyboard.GetState().IsKeyDown(Keys.Up) && measureLine > 0 then 
                            measureLine <-  measureLine-1
                        if Keyboard.GetState().IsKeyDown(Keys.P) && not(Ppressed) then
                            Ppressed <-true
                            if frontDone = 3 then
                                backPointCloud <- currentPointCloud.Clone
                                //this.generatePointCloud(distancesArray, true, backPointCloud)
                                frontDone <- 1
                                backPointCloud.ConvertToOBJ("backPoints") 
                            elif frontDone = 2 then
                                System.Diagnostics.Debug.WriteLine(Array.max(distancesArray))
                                sidePointCloud <- currentPointCloud.Clone
                                //this.generatePointCloud(distancesArray, true, sidePointCloud)
                                frontDone <- 3
                                sidePointCloud.ConvertToOBJ("sidePoints")
                            elif frontDone = 1 then
                                System.Diagnostics.Debug.WriteLine(Array.max(distancesArray))
                                frontPointCloud <- currentPointCloud.Clone
                                //this.generatePointCloud( distancesArray, false, frontPointCloud)
                                frontDone <- 2
                                frontPointCloud.ConvertToOBJ("frontPoints")
                        if Keyboard.GetState().IsKeyUp(Keys.P) then
                            Ppressed <- false
                
                        base.Update gameTime
            
                    member this.Uninitalize =
                        nui.Uninitialize ()
            
                    member this.grabPoints =
                        let mutable theLastBuiltPointCloud = currentPointCloud
                        if not(theLastBuiltPointCloud.Built) then
                            theLastBuiltPointCloud <- lastPointCloud
                        if frontDone = 3 then
                            backPointCloud <- theLastBuiltPointCloud.Clone
                            //this.generatePointCloud(distancesArray, true, backPointCloud)
                            frontDone <- 1
                            let rotationAxis = (Vector3.Subtract(backPointCloud.Head, backPointCloud.Hip))
                            let rotationMatrix = Matrix.CreateFromAxisAngle(rotationAxis, (float32)Math.PI)
                            backPointCloud.Transform(rotationMatrix)
                            backPointCloud.ConvertToOBJ("backPoints") 
                            
                        elif frontDone = 2 then
                            sidePointCloud <- theLastBuiltPointCloud.Clone
                            //this.generatePointCloud(distancesArray, true, sidePointCloud)
                            frontDone <- 3
                            sidePointCloud.ConvertToOBJ("sidePoints")
                        elif frontDone = 1 then
                            System.Diagnostics.Debug.WriteLine(Array.max(distancesArray))
                            frontPointCloud <- theLastBuiltPointCloud.Clone
                            //this.generatePointCloud( distancesArray, false, frontPointCloud)
                            frontDone <- 2
                            frontPointCloud.ConvertToOBJ("frontPoints")
            

                    member this.DepthImg 
                        with get()=img

                    member this.LineDepths 
                        with get()= 
                            let mutable newArray = Array.create distancesOnLine.Length 0
                            Array.Copy(distancesOnLine, newArray, distancesOnLine.Length)
                            let noZero = Array.filter (fun a -> not(a=0)) newArray //remove any 0 elements
                            noZero

            
 
            
        let game = new XnaGame()
        game.Run()
