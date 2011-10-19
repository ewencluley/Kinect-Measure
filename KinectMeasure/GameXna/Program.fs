// Learn more about F# at http://fsharp.net

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Media

open Microsoft.Research.Kinect.Nui
open System.Linq
open System

type XnaGame() as this =
    inherit Game()
    
    let screenWidth = 640
    let screenHeight = 480

    do this.Content.RootDirectory <- "XnaGameContent"
    let graphicsDeviceManager = new GraphicsDeviceManager(this)

    let mutable frontMeasure:int[] = null
    let mutable backMeasure:int[] = null

    let mutable singlePress = true

    let mutable sprite : Texture2D = null
    let mutable depthImg : Texture2D = null
    let mutable dotImg : Texture2D = null

    let mutable spriteBatch : SpriteBatch = null
    let mutable leftHandPos = new Vector3(0.0f,0.0f, 0.0f)
    let mutable rightHandPos = new Vector3(0.0f,0.0f, 0.0f)
    let mutable headPos = new Vector3(0.0f,0.0f, 0.0f)
    let mutable leftFootPos = new Vector3(0.0f,0.0f, 0.0f)
    let mutable rightFootPos = new Vector3(0.0f,0.0f, 0.0f)

    let mutable timeSinceLastTick = 0

    let mutable clickSound:Song = null

    let mutable measurementFont:SpriteFont =null
    let mutable measurement =0.0

    let kinect = new KinectSensor(this)

    let measureSurface (points:int[]) =
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
        dotImg <- game.Content.Load<Texture2D>("dot")
        measurementFont <- game.Content.Load<SpriteFont>("font1")
        clickSound <- game.Content.Load<Song>("click")

    override game.Update gameTime = 
        
        if Keyboard.GetState().IsKeyDown(Keys.A) && singlePress then
            singlePress <- not(singlePress)
            if frontMeasure = null then 
                frontMeasure <- kinect.LineDepths
                measurement <- measureSurface frontMeasure
            elif backMeasure = null then
                backMeasure <- kinect.LineDepths
                measurement <- measurement + (measureSurface backMeasure)
        if Keyboard.GetState().IsKeyUp(Keys.A) then singlePress <- true
        
        
        
        base.Update gameTime

    override game.Draw gameTime = 
        game.GraphicsDevice.Clear(Color.CornflowerBlue)
        spriteBatch.Begin()
        if not(depthImg = null) then spriteBatch.Draw(depthImg, new Rectangle(0, 0, screenWidth/2, screenHeight/2), Color.White)
        spriteBatch.Draw(sprite, new Vector2(leftHandPos.X, leftHandPos.Y), Color.White)
        spriteBatch.Draw(sprite, new Vector2(rightHandPos.X, rightHandPos.Y), Color.White)
        spriteBatch.Draw(sprite, new Vector2(headPos.X, headPos.Y), Color.White)
        spriteBatch.Draw(sprite, new Vector2(leftFootPos.X, leftFootPos.Y), Color.White)
        spriteBatch.Draw(sprite, new Vector2(rightFootPos.X, rightFootPos.Y), Color.White)

        if not(frontMeasure = null) then
            for i = 0 to frontMeasure.Length-1 do
                spriteBatch.Draw(dotImg, new Vector2(float32(i*2), float32(frontMeasure.[i]/3)), (if frontMeasure.[i]>0 then Color.White else Color.Black))
        if not(backMeasure = null) then
            for i = 0 to backMeasure.Length-1 do
                spriteBatch.Draw(dotImg, new Vector2(float32(i*2), float32(backMeasure.[i]/3)), Color.DarkOliveGreen)

        spriteBatch.DrawString(measurementFont, measurement.ToString(), new Vector2(0.0f, 440.0f), Color.White);

        spriteBatch.End()

    member game.PassJoints leftHand rightHand head leftFoot rightFoot=
        leftHandPos <- leftHand
        rightHandPos <- rightHand
        headPos <- head
        leftFootPos <- leftFoot
        rightFootPos <- rightFoot
    member game.SetDepthImg img=
        depthImg <- img

    member game.ScreenDimensions
        with get() = (screenWidth, screenHeight)

and KinectSensor(game:XnaGame)=
            inherit DrawableGameComponent(game)
            
            let mutable distancesOnLine = Array.create (fst (game.ScreenDimensions)) 0
            let mutable img:Texture2D = null
            let mutable skeleton:SkeletonData = null

            let Scale ( maxPixel:int, maxSkeleton:float, position:float)=
                let mutable scale = (((((float) maxPixel / maxSkeleton) / 2.0) * position) + (float)(maxPixel/2))
                if scale > (float)maxPixel then
                    scale <- (float)maxPixel
                if scale < (float)0 then
                    scale <- (float)0
                (float32)scale

            let ScaleTo (joint:Joint, width, height, skeletonMaxX, skeletonMaxY)=
                new Vector3(Scale(width, skeletonMaxX, (float)joint.Position.X), Scale(height, skeletonMaxY, -(float)joint.Position.Y), joint.Position.Z)
            
            let SkeletonReady (sender : obj) (args: SkeletonFrameReadyEventArgs)=
                let mutable i=0
                let mutable skeleton = null
                while skeleton = null && i< args.SkeletonFrame.Skeletons.Length do
                    if args.SkeletonFrame.Skeletons.ElementAt(i).TrackingState = SkeletonTrackingState.Tracked then
                        skeleton <- args.SkeletonFrame.Skeletons.ElementAt(i)
                        let leftHand = ScaleTo(skeleton.Joints.[JointID.HandLeft], 640, 480, 1.0, 1.0) //in f# ".[i]" is notation for accessing array element, not simply "[i]"
                        let rightHand = ScaleTo(skeleton.Joints.[JointID.HandRight], 640, 480, 1.0, 1.0)
                        let head = ScaleTo(skeleton.Joints.[JointID.Head], 640, 480, 1.0, 1.0)
                        let leftFoot = ScaleTo(skeleton.Joints.[JointID.FootLeft ], 640, 480, 1.0, 1.0)
                        let rightFoot = ScaleTo(skeleton.Joints.[JointID.FootRight ], 640, 480, 1.0, 1.0)
                        game.PassJoints leftHand rightHand head leftFoot rightFoot
                    i<-i+1
            let mutable measureLine =100  
              
            let DepthReady (sender : obj) (args:ImageFrameReadyEventArgs)=
                
                let maxDist = 4000
                let minDist = 850
                let distOffset = maxDist - minDist

                let pImg = args.ImageFrame.Image
                img <- new Texture2D(game.GraphicsDevice, pImg.Width, pImg.Height)
                let DepthColor = Array.create (pImg.Width*pImg.Height) (new Color(255,255,255))

                //distancesOnLine <- Array.create (fst (game.ScreenDimensions)) 0

                for y = 0 to pImg.Height-1 do
                    for x = 0 to pImg.Width-1 do
                        let n = (y * pImg.Width + x) * 2
                        let distance = (int pImg.Bits.[n + 0] >>>3) ||| (int pImg.Bits.[n + 1] <<< 5) //put together bit data as depth
                        let pI = int (pImg.Bits.[n] &&& 7uy) // gets the player index
                        let mutable intensity = 255-(255*Math.Max(int(distance-minDist),0)/distOffset) //convert distance into a gray level value between 0 and 255 taking into account min and max distances of the kinect.
                        let mutable colour = new Color(intensity, intensity, intensity)
                        if y=measureLine && pI > 0 then
                            distancesOnLine.[x] <- distance
                            colour <- new Color(255, 0, 0)
                        if (int) pI = 0 then //if not a player
                            distancesOnLine.[x] <- 0
                            colour <- new Color(0, 0, 0)
                        DepthColor.[y * pImg.Width + x] <- colour
                img.SetData(DepthColor)
                game.SetDepthImg img


            let nui = new Runtime()
            do nui.Initialize(RuntimeOptions.UseSkeletalTracking ||| RuntimeOptions.UseDepthAndPlayerIndex)
            //do nui.SkeletonEngine.TransformSmooth <- true;
            //do nui.SkeletonFrameReady.AddHandler(new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonReady))
            do nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex)
            do nui.DepthFrameReady.AddHandler(new EventHandler<ImageFrameReadyEventArgs>(DepthReady))

            override this.Draw gameTime=
                base.Draw gameTime

            override this.Update gameTime=
                if Keyboard.GetState().IsKeyDown(Keys.Down) && measureLine < snd(game.ScreenDimensions)/2 then 
                    measureLine <-  measureLine+1
                if Keyboard.GetState().IsKeyDown(Keys.Up) && measureLine > 0 then 
                    measureLine <-  measureLine-1
                base.Update gameTime
            
            member this.Uninitalize =
                nui.Uninitialize ()

            member this.DepthImg with get()=img

            member this.LineDepths 
                with get()= 
                    
                    let mutable newArray = Array.create distancesOnLine.Length 0
                    Array.Copy(distancesOnLine, newArray, distancesOnLine.Length)
                    newArray
                   
 
            
let game = new XnaGame()
game.Run()
