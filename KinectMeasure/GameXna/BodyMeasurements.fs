namespace BodyData

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

    type BodyMeasurements(hea, shL, shR, shC, hiL, hiR, hiC, foL, foR)=
        
        let phi = 1.641 //golden ratio
        
        //the depth image of the player.  it should only contain one player and each depth should be an int from  850 - 4000 (the valid depths for the depths)  
        let mutable depthImage:int[] = Array.create 76800 0
        
        //joints
        let mutable head = hea
        let mutable shoulderL = shL
        let mutable shoulderR = shR
        let mutable shoulderC = shC
        let mutable hipL = hiL
        let mutable hipR = hiR
        let mutable hipC = hiC
        let mutable footL = foL
        let mutable footR = foR

       

        new() = BodyMeasurements(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f))
        
        member this.completeBody =  //complete body if has joints and depth data
            if Array.max(depthImage) > 0 then
                if not(head.Equals(new Vector3(0.0f,0.0f,0.0f))) then
                    true
                else
                    false
            else
                false

        member this.maxDepth = Array.max(depthImage) //used to check if the depth array has any data
        
        //These members find the top and bottom most points of the depth image
        //The values they return are based on the 2D visualisation space i.e. in the range x=0-240, y=0-320
        member this.GetTopOfHead =
            let mutable topOfHead = Unchecked.defaultof<Vector3>
            let mutable y = 0
            while y < 239 && topOfHead.Equals(Unchecked.defaultof<Vector3>) do
                let mutable x = 0
                while x < 319 && topOfHead.Equals(Unchecked.defaultof<Vector3>) do
                    let arrayPosition = y * 320 + x  
                    let depth = depthImage.[arrayPosition]
                    if depth > 0 then
                        let coordinates = new Vector3(float32 x, float32 y, float32 depth)
                        //check it is not a hand raised above the head
                        let closeEnoughToHead = 
                            let euclidDist = Vector2.Distance(new Vector2(head.X, head.Y), new Vector2(coordinates.X, coordinates.Y))
                            if euclidDist < 50.0f then
                                true
                            else
                                false
                        if closeEnoughToHead then
                            topOfHead <- coordinates
                            System.Diagnostics.Debug.WriteLine("TopOfHead=" + topOfHead.ToString())
                    x <- x + 1
                y <- y + 1
            topOfHead

        member this.GetBottomOfFeet =
            let mutable bottomOfFeet = Unchecked.defaultof<Vector3>
            let mutable y = 0
            while y < 239 && bottomOfFeet.Equals(Unchecked.defaultof<Vector3>) do
                let mutable x = 0
                while x < 319 && bottomOfFeet.Equals(Unchecked.defaultof<Vector3>) do
                    let arrayPosition = 76799 - (y * 320 + x)  
                    let depth = depthImage.[arrayPosition]
                    if depth > 0 then
                        let coordinates = new Vector3(320.0f - float32 x, 240.0f - float32 y, float32 depth)
                        let closeEnoughToHead = 
                            let euclidDist = Vector2.Distance(new Vector2(head.X, head.Y), new Vector2(coordinates.X, coordinates.Y))
                            if euclidDist < 100.0f then
                                true
                            else
                                false
                        //if closeEnoughToHead then
                        bottomOfFeet <- coordinates
                        System.Diagnostics.Debug.WriteLine("BottomOfFeet=" + bottomOfFeet.ToString())
                    x <- x + 1
                y <- y + 1
            bottomOfFeet

        //*******************************
        //Measurement members. Used to find points at which measurements should be taken
        //*******************************

        //Height measurement
        member this.MeasureHeightVis=
            this.GetTopOfHead.Y - this.GetBottomOfFeet.Y
        
        member this.MeasureHeightWorld=
            this.GetTopOfHead.Y * 5.0f - this.GetBottomOfFeet.Y * 5.0f
        
        //Floor to waist measurement
        member this.MeasureToWaistVis=
            this.MeasureHeightVis / float32 phi

        member this.MeasureToWaistWorld=
            this.MeasureHeightWorld / float32 phi

        member this.SetDepthData dI =
            depthImage <- dI;

        member this.SetSkeleton (hea, shL, shR, shC, hiL, hiR, hiC, foL, foR) =
            head <- hea
            shoulderL <- shL
            shoulderR <- shR
            shoulderC <- shC
            hipL <- hiL
            hipR <- hiR
            hipC <- hiC
            footL <- foL
            footR <- foR
            


    

