using System;


public abstract class EntityAnimation {
  public bool isCritical;

  public bool isAnimating = true;

  protected float deltaTime;
  public float time;

  
  public EntityAnimation(bool isCritical) {
    this.isCritical = isCritical;
  }


  public void Update(float deltaTime) {
    this.deltaTime = deltaTime;
    this.time += deltaTime;
    Update();
  }

  protected abstract void Update();


  protected void FinishAnimation() {
    isAnimating = false;
  }
}
