// // See https://aka.ms/new-console-template for more information
// using Microsoft.ML.OnnxRuntime;
// using Microsoft.ML.OnnxRuntime.Tensors;
// using SixLabors.ImageSharp;
// using SixLabors.ImageSharp.Drawing.Processing;
// using SixLabors.ImageSharp.PixelFormats;
// using SixLabors.ImageSharp.Processing;
// using SixLabors.Fonts;

// var imagePath = "test_fixtures/target_A.jpg";
// var modelPath = "test_fixtures/best.onnx";
// var classNamesPath = "test_fixtures/class_names.txt";
// var outputPath = "test_fixtures/output.jpg";


// // Load the ONNX model
// using var session = new InferenceSession(modelPath);
// Console.WriteLine(session.InputMetadata.Keys.First());


// using var image = Image.Load<Rgb24>(imagePath);
// var originalWidth = image.Width;
// var originalHeight = image.Height;
// using var targetImage = new Image<Rgb24>(1920, 1920, Color.Gray);
// float scaleX = (float)1920 / originalWidth;
// float scaleY = (float)1920 / originalHeight;
// float scale = Math.Min(scaleX, scaleY);

// int newWidth = (int)(originalWidth * scale);
// int newHeight = (int)(originalHeight * scale);
// Console.WriteLine($"Resizing image from {originalWidth}x{originalHeight} to {newWidth}x{newHeight} with padding.");
// image.Mutate(x => x.Resize(newWidth, newHeight));
// targetImage.Mutate(x => x.DrawImage(image, new Point(0, 0), 1f));


// var tensor = new DenseTensor<float>(new[] { 1, 3, 1920, 1920 });



// targetImage.ProcessPixelRows(accessor =>
// {
//     for (int y = 0; y < 1920; y++)
//     {
//         Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
//         for (int x = 0; x < 1920; x++)
//         {
//             var pixel = pixelRow[x];

//             tensor[0, 0, y, x] = pixel.R / 255f;
//             tensor[0, 1, y, x] = pixel.G / 255f;
//             tensor[0, 2, y, x] = pixel.B / 255f;
//         }
//     }
// });

// var _inputName = session.InputMetadata.Keys.First();
// var inputs = new List<NamedOnnxValue>
//         {
//             NamedOnnxValue.CreateFromTensor(_inputName, tensor)
//         };
// using var results = session.Run(inputs);
// var output = results.First().AsTensor<float>();
// Console.WriteLine($"Output dimensions: {string.Join(", ", output.Dimensions[1])}");
// // Process the output tensor yolo style onnx output, separate boxes, scores, and class indices
// var boxes = new List<float[]>();
// var scores = new List<float>();
// var classIndices = new List<int>();
// for (int i = 0; i < output.Dimensions[1]; i++)
// {
//     if (output[0, i, 4] < 0.5) continue; // Filter out low confidence scores
//     scores.Add(output[0, i, 4]);
//     classIndices.Add((int)output[0, i, 5]);
//     var box = new float[4];
//     for (int j = 0; j < 4; j++)
//     {
//         box[j] = output[0, i, j];
//     }
//     boxes.Add(box);
// }


// List<string> classNames = [.. File.ReadAllLines(classNamesPath)];
// // Print the results
// for (int i = 0; i < boxes.Count; i++)
// {
//     Console.WriteLine($"Box: {string.Join(", ", boxes[i])}, Score: {scores[i]}, Class Index: {classNames[classIndices[i]]}");
// }

// // Mark the detected objects on the image
// var font = SystemFonts.CreateFont("Arial", 24); // Create font once
// try
// {
//     targetImage.Mutate(x =>
//     {
//         for (int i = 0; i < boxes.Count; i++)
//         {
//             var box = boxes[i]; // [x1, y1, x2, y2] all in pixels
//             var score = scores[i];
//             var classIndex = classIndices[i];
//             var className = classNames[classIndex];

//             // Mark the boxes using ImageSharp
//             var x1 = (int)box[0];
//             var y1 = (int)box[1];
//             var x2 = (int)box[2];
//             var y2 = (int)box[3];
//             var w = (int)(x2 - x1);
//             var h = (int)(y2 - y1);
//             Console.WriteLine($"Drawing box {i}: {className} at ({x1}, {y1}), width: {w}, height: {h}, score: {score}");
//             var rect = new Rectangle(x1, y1, w, h);
//             x.Draw(Color.Green, 2f, rect);
//         }
//         // Draw the class names and scores
//         for (int i = 0; i < boxes.Count; i++)
//         {
//             var box = boxes[i];
//             var x1 = (int)box[0];
//             var y1 = (int)box[1];
//             var className = classNames[classIndices[i]];
//             var score = scores[i];
//             var text = $"{className}: {score:F2}";
//             var fontOptions = new RichTextOptions(font)
//             {
//                 Origin = new PointF(x1, y1 - 20),
//             };
//             x.DrawText(fontOptions, text, Color.White);
//         }
//     });
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"Exception during image mutation: {ex}");
//     throw;
// }


// // Save the output image
// targetImage.Save(outputPath);