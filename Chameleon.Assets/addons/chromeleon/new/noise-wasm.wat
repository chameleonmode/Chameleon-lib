// noise-wasm.wat - WebAssembly text format
// This needs to be compiled to .wasm
// You can use tools like wat2wasm or WebAssembly Studio

(module
  (memory (export "memory") 1)
  
  ;; Function to add noise to image data
  ;; Params: buffer pointer, noise level
  (func (export "addNoise") (param $buffer i32) (param $level i32)
    (local $i i32)
    (local $len i32)
    (local $r f32)
    (local $noiseAmount f32)
    
    ;; Calculate noise amount from level (0-10)
    (set_local $noiseAmount (f32.div 
      (f32.convert_i32_s (get_local $level))
      (f32.const 10.0)
    ))
    
    ;; Get buffer length (assuming it's passed correctly)
    (set_local $len (i32.load (get_local $buffer)))
    
    ;; Initialize counter
    (set_local $i (i32.const 0))
    
    ;; Loop through buffer, adjusting every 4 bytes (RGBA)
    (block $done
      (loop $loop
        ;; Check if we've reached the end
        (br_if $done (i32.ge_u (get_local $i) (get_local $len)))
        
        ;; Only modify RGB, not Alpha
        ;; Red component
        (set_local $r (call $random))
        (f32.store 
          (i32.add (get_local $buffer) (get_local $i))
          (f32.max 
            (f32.const 0.0)
            (f32.min
              (f32.const 255.0)
              (f32.add
                (f32.load (i32.add (get_local $buffer) (get_local $i)))
                (f32.mul
                  (f32.sub
                    (f32.mul (get_local $r) (f32.const 2.0))
                    (f32.const 1.0)
                  )
                  (get_local $noiseAmount)
                )
              )
            )
          )
        )
        
        ;; Green component (i+1)
        (set_local $r (call $random))
        (f32.store 
          (i32.add (get_local $buffer) (i32.add (get_local $i) (i32.const 1)))
          (f32.max 
            (f32.const 0.0)
            (f32.min
              (f32.const 255.0)
              (f32.add
                (f32.load (i32.add (get_local $buffer) (i32.add (get_local $i) (i32.const 1))))
                (f32.mul
                  (f32.sub
                    (f32.mul (get_local $r) (f32.const 2.0))
                    (f32.const 1.0)
                  )
                  (get_local $noiseAmount)
                )
              )
            )
          )
        )
        
        ;; Blue component (i+2)
        (set_local $r (call $random))
        (f32.store 
          (i32.add (get_local $buffer) (i32.add (get_local $i) (i32.const 2)))
          (f32.max 
            (f32.const 0.0)
            (f32.min
              (f32.const 255.0)
              (f32.add
                (f32.load (i32.add (get_local $buffer) (i32.add (get_local $i) (i32.const 2))))
                (f32.mul
                  (f32.sub
                    (f32.mul (get_local $r) (f32.const 2.0))
                    (f32.const 1.0)
                  )
                  (get_local $noiseAmount)
                )
              )
            )
          )
        )
        
        ;; Skip alpha component (i+3)
        
        ;; Increment i by 4 (RGBA)
        (set_local $i (i32.add (get_local $i) (i32.const 4)))
        
        ;; Continue loop
        (br $loop)
      )
    )
  )
  
  ;; Simple PRNG function
  ;; Returns value between 0 and 1
  (func $random (result f32)
    (local $seed i32)
    
    ;; Load current seed from memory (offset 0)
    (set_local $seed (i32.load (i32.const 0)))
    
    ;; Update seed with LCG algorithm
    (set_local $seed 
      (i32.add
        (i32.mul
          (get_local $seed)
          (i32.const 1664525)
        )
        (i32.const 1013904223)
      )
    )
    
    ;; Store updated seed
    (i32.store (i32.const 0) (get_local $seed))
    
    ;; Convert to float between 0 and 1
    (f32.div
      (f32.convert_i32_u (get_local $seed))
      (f32.const 4294967295.0) ;; 2^32 - 1
    )
  )
  
  ;; Initialize seed with a starting value
  (func $init
    (i32.store (i32.const 0) (i32.const 12345))
  )
  
  ;; Call init function
  (start $init)
)